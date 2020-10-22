// <copyright file="CreateEnvironmentResourceJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using ResourceType = Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.ResourceType;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Create Environment Resource Job Handler. It creates environemnts for hot pool.
    /// </summary>
    public class CreateEnvironmentResourceJobHandler : EnvironmentContinuationJobHandlerBase<CreateEnvironmentResourceJobHandler.Payload, CreateEnvironmentResourceJobHandler.JobState, CreateEnvironmentResourceJobHandler.CreateEnvironmentResourceResult>
    {
        /// <summary>
        /// Default queue id.
        /// </summary>
        public const string DefaultQueueId = "jobhandler-create-environment";
        private const CloudEnvironmentState TargetState = CloudEnvironmentState.Created;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartEnvironmentContinuationJobHandlerV2"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">target env repo.</param>
        /// <param name="heartbeatRepository">target env heartbeat repo.</param>
        /// <param name="resourceBrokerHttpClient">Target Resource Broker Http Client.</param>
        /// <param name="resourceAllocationManager">target resource allocation manager.</param>
        /// <param name="resourceSelector">Resource selector.</param>
        /// <param name="jobQueueProducerFactory">Job Queue producer factory instance.</param>
        public CreateEnvironmentResourceJobHandler(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            ICloudEnvironmentHeartbeatRepository heartbeatRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            IResourceAllocationManager resourceAllocationManager,
            IResourceSelectorFactory resourceSelector,
            IJobQueueProducerFactory jobQueueProducerFactory)
            : base(cloudEnvironmentRepository, jobQueueProducerFactory)
        {
            ResourceBrokerHttpClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
            ResourceAllocationManager = Requires.NotNull(resourceAllocationManager, nameof(resourceAllocationManager));
            ResourceSelector = Requires.NotNull(resourceSelector, nameof(resourceSelector));
            HeartbeatRepository = Requires.NotNull(heartbeatRepository, nameof(heartbeatRepository));
        }

        /// <inheritdoc/>
        public override string QueueId => DefaultQueueId;

        /// <inheritdoc/>
        protected override string LogBaseName => EnvironmentLoggingConstants.ContinuationTaskMessageHandlerCreateEnv;

        /// <inheritdoc/>
        protected override EnvironmentOperation Operation => EnvironmentOperation.Provisioning;

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerHttpClient { get; }

        private IResourceAllocationManager ResourceAllocationManager { get; }

        private IResourceSelectorFactory ResourceSelector { get; }

        private ICloudEnvironmentHeartbeatRepository HeartbeatRepository { get; }

        /// <inheritdoc/>
        protected override JobState GetStateFromPayload(Payload payload)
        {
            return payload.CurrentState;
        }

        /// <inheritdoc/>
        protected override Task<bool> FailOperationShouldTriggerCleanupAsync(IEntityRecordRef<CloudEnvironment> record, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        protected override TransitionState FetchOperationTransition(Payload payload, IEntityRecordRef<CloudEnvironment> record, IDiagnosticsLogger logger)
        {
            return record.Value.Transitions.Creating;
        }

        /// <inheritdoc/>
        protected override async Task FailOperationCleanupCoreAsync(
            Payload operationInput,
            IEntityRecordRef<CloudEnvironment> record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            logger.AddCloudEnvironment(record.Value);
            logger.LogError($"{LogBaseName}_failed");

            // Mark environemnt deleted
            var didUpdate = await CloudEnvironmentRepository.UpdateRecordAsync(
                               operationInput.EnvironmentId,
                               record,
                               (environment, innerLogger) =>
                               {
                                   // Update state to be failed
                                   environment.IsDeleted = true;
                                   environment.LastDeleted = DateTime.UtcNow;
                                   return Task.FromResult(true);
                               },
                               logger,
                               LogBaseName);

            if (!didUpdate)
            {
                logger.LogError($"{LogBaseName}_failed_to_mark_environment_deleted");
            }

            var resourceList = new List<Guid>();

            // Delete the allocated resources.
            if (record.Value.Compute != default)
            {
                resourceList.Add(record.Value.Compute.ResourceId);
            }

            if (record.Value.Storage != default)
            {
                resourceList.Add(record.Value.Storage.ResourceId);
            }

            if (record.Value.OSDisk != default)
            {
                resourceList.Add(record.Value.OSDisk.ResourceId);
            }

            if (resourceList.Count != 0)
            {
                try
                {
                    await ResourceBrokerHttpClient.DeleteAsync(Guid.Parse(record.Value.Id), resourceList, logger.NewChildLogger());
                }
                catch (Exception ex)
                {
                    logger.LogException($"{LogBaseName}_failed_to_delete_environment_resources", ex);
                }
            }

            // delete heartbeat record if exists.
            if (!string.IsNullOrEmpty(record.Value.HeartbeatResourceId))
            {
                await HeartbeatRepository.DeleteAsync(record.Value.HeartbeatResourceId, logger.NewChildLogger());
            }

            // delete environment
            didUpdate = await CloudEnvironmentRepository.DeleteAsync(operationInput.EnvironmentId.ToString(), logger);

            if (!didUpdate)
            {
                logger.LogError($"{LogBaseName}_failed_to_delete_environment_record");
            }
        }

        /// <inheritdoc/>
        protected override async Task<IEntityRecordRef<CloudEnvironment>> FetchReferenceAsync(Payload payload, IDiagnosticsLogger logger)
        {
            if (!payload.IsCreated)
            {
                var envRecordRef = await CreateReferenceAsync(payload, logger);
                payload.IsCreated = true;
                return envRecordRef;
            }

            return await base.FetchReferenceAsync(payload, logger);
        }

        private async Task<IEntityRecordRef<CloudEnvironment>> CreateReferenceAsync(Payload payload, IDiagnosticsLogger logger)
        {
            var cloudEnvRecord = new CloudEnvironment()
            {
                Id = payload.EnvironmentId.ToString(),
                Type = Common.Contracts.EnvironmentType.CloudEnvironment,
                FriendlyName = string.Empty,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                OwnerId = string.Empty,
                SkuName = payload.Pool.Details.SkuName,
                Location = payload.Pool.Details.Location,
                PoolReference = new CloudEnvironmentPoolDefinition() { Code = payload.Pool.Details.GetPoolDefinition(), },
                QueueResourceAllocation = true,
                IsAssigned = false,
                IsReady = false,
                State = CloudEnvironmentState.Created,
            };

            // await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvRecord, CloudEnvironmentState.Created, nameof(CreateEnvironmentResourceJobHandler), payload.Reason, false, logger.NewChildLogger());

            cloudEnvRecord = await CloudEnvironmentRepository.CreateAsync(cloudEnvRecord, logger.NewChildLogger());

            return new EnvironmentRecordRef(cloudEnvRecord);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationJobResult<JobState, CreateEnvironmentResourceResult>> ContinueAsync(
            Payload payload,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken)
        {
            // Add environment id and resource ids to logger
            LogResource(payload, record.Value, logger);

            if (!IsValidState(record, payload))
            {
                return ReturnFailed($"FailedEnvironmentStartState record in invalid state '{record.Value.State}'");
            }

            // Run operation
            switch (payload.CurrentState)
            {
                case JobState.AllocateResource:
                    // Trigger compute allocate by calling allocate endpoint
                    return await RunAllocateResourceAsync(payload, record, logger);

                case JobState.CheckResourceState:
                    // Trigger check resource state
                    return await RunCheckResourceStateAsync(payload, record, logger);

                case JobState.StartHeartbeatMonitoring:
                    // Start environment monitoring.
                    return await RunStartEnvironmentMonitoring(payload, record, logger);

                default:
                    return ReturnFailed("InvalidEnvironmentCreateState");
            }
        }

        private static bool IsValidState(IEntityRecordRef<CloudEnvironment> record, Payload operationInput)
        {
            return record.Value.State == TargetState;
        }

        private async Task<ContinuationJobResult<JobState, CreateEnvironmentResourceResult>> RunAllocateResourceAsync(
            Payload operationInput,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger)
        {
            var requests = await ResourceSelector.CreateAllocationRequestsAsync(
                                record.Value,
                                logger);

            var resultResponse = await ResourceAllocationManager.AllocateResourcesAsync(
                                     Guid.Parse(record.Value.Id),
                                     requests,
                                     logger.NewChildLogger());

            var computeResponse = resultResponse.Single(x => x.Type == ResourceType.ComputeVM);
            var osDiskResponse = resultResponse.SingleOrDefault(x => x.Type == ResourceType.OSDisk);
            var storageResponse = resultResponse.SingleOrDefault(x => x.Type == ResourceType.StorageFileShare);

            var didUpdate = await CloudEnvironmentRepository.UpdateRecordAsync(
                                operationInput.EnvironmentId,
                                record,
                                (environment, innerLogger) =>
                                {
                                    // Update compute and disk resources
                                    record.Value.Compute = computeResponse;
                                    if (osDiskResponse != default)
                                    {
                                        record.Value.OSDisk = osDiskResponse;
                                    }

                                    // For archived environments, dont switch storage resource.
                                    if (storageResponse != default)
                                    {
                                        record.Value.Storage = storageResponse;
                                    }

                                    return Task.FromResult(true);
                                },
                                logger,
                                LogBaseName);

            if (!didUpdate)
            {
                return ReturnFailed(ResultFromReason("FailedToUpdateEnvironmentRecord"));
            }

            LogResource(operationInput, record.Value, logger);

            return ReturnNextState(JobState.CheckResourceState);
        }

        private async Task<ContinuationJobResult<JobState, CreateEnvironmentResourceResult>> RunCheckResourceStateAsync(
            Payload operationInput,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger)
        {
            var environmentTransition = new EnvironmentTransition(record.Value);
            var hasStorageResource = environmentTransition.Value.Storage != default;
            var hasOSDiskResource = environmentTransition.Value.OSDisk != default;

            var resourceList = new List<Guid>() { environmentTransition.Value.Compute.ResourceId };
            if (hasStorageResource)
            {
                resourceList.Add(environmentTransition.Value.Storage.ResourceId);
            }

            if (hasOSDiskResource)
            {
                resourceList.Add(environmentTransition.Value.OSDisk.ResourceId);
            }

            var statusResponse = await ResourceBrokerHttpClient.StatusAsync(
                operationInput.EnvironmentId,
                resourceList,
                logger.NewChildLogger());

            var computeStatus = statusResponse.Single(x => x.Type == ResourceType.ComputeVM);
            var osDiskStatus = statusResponse.SingleOrDefault(x => x.Type == ResourceType.OSDisk);
            var storageStatus = statusResponse.SingleOrDefault(x => x.Type == ResourceType.StorageFileShare);

            var updatedResourceList = new List<Guid>();
            var resourceRecord = environmentTransition.Value.Compute;
            UpdateResourceStatus(environmentTransition, resourceRecord, computeStatus, updatedResourceList);

            if (hasOSDiskResource)
            {
                resourceRecord = environmentTransition.Value.OSDisk;
                UpdateResourceStatus(environmentTransition, resourceRecord, osDiskStatus, updatedResourceList);
            }

            if (hasStorageResource)
            {
                resourceRecord = environmentTransition.Value.Storage;
                UpdateResourceStatus(environmentTransition, resourceRecord, storageStatus, updatedResourceList);
            }

            // Commit resource status changes to database.
            await CloudEnvironmentRepository.UpdateTransitionAsync("Environment", environmentTransition, logger.NewChildLogger());
            record.Value = environmentTransition.Value;

            if (updatedResourceList.Count > 0)
            {
                try
                {
                    await ResourceBrokerHttpClient.DeleteAsync(Guid.Parse(environmentTransition.Value.Id), updatedResourceList, logger.NewChildLogger());
                }
                catch (Exception ex)
                {
                    // Continue on failure to delete shadow record, as it is best effort.
                    logger.LogException($"{LogBaseName}_delete_shadow_record_error", ex);
                }
            }

            LogResource(operationInput, environmentTransition.Value, logger);

            bool resourcesReady = statusResponse.All(status => status.IsReady);

            if (resourcesReady)
            {
                return ReturnNextState(JobState.StartHeartbeatMonitoring);
            }
            else if (hasStorageResource && storageStatus.ProvisioningStatus.IsFailedState())
            {
                return ReturnFailed(ResultFromReason("InvalidStorageResourceState"));
            }
            else if (hasOSDiskResource && osDiskStatus.ProvisioningStatus.IsFailedState())
            {
                return ReturnFailed(ResultFromReason("InvalidOSDiskResourceState"));
            }
            else if (computeStatus.ProvisioningStatus.IsFailedState())
            {
                return ReturnFailed(ResultFromReason("InvalidComputeResourceState"));
            }

            return ReturnRetry(TimeSpan.FromSeconds(10));
        }

        public async Task<ContinuationJobResult<JobState, CreateEnvironmentResourceResult>> RunStartEnvironmentMonitoring(
            Payload operationInput,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger)
        {
            if (record.Value.IsReady.HasValue && record.Value.IsReady.Value)
            {
                return ReturnSucceeded();
            }

            // Create heartbeat record.
            var heartbeatResourceId = string.Empty;
            if (string.IsNullOrEmpty(record.Value.HeartbeatResourceId))
            {
                var heartbeatRecord = new CloudEnvironmentHeartbeat();
                heartbeatRecord = await HeartbeatRepository.CreateAsync(heartbeatRecord, logger.NewChildLogger());
                heartbeatResourceId = heartbeatRecord.Id;
            }
            else
            {
                heartbeatResourceId = record.Value.HeartbeatResourceId;
            }

            // TODO:: Kick off heartbeat monitoring, once : https://github.com/microsoft/vssaas-planning/issues/1051, is done

            // Update heartbeat record id and mark resource ready
            var didUpdate = await CloudEnvironmentRepository.UpdateRecordAsync(
                                operationInput.EnvironmentId,
                                record,
                                (environment, innerLogger) =>
                                {
                                    // Update heartbeat record id.
                                    record.Value.HeartbeatResourceId = heartbeatResourceId;

                                    // mark resource ready
                                    record.Value.IsReady = true;
                                    record.Value.Ready = DateTime.UtcNow;

                                    return Task.FromResult(true);
                                },
                                logger,
                                LogBaseName);

            if (!didUpdate)
            {
                return ReturnFailed(ResultFromReason("FailedToUpdateEnvironmentRecord"));
            }

            return ReturnSucceeded();
        }

        private static void LogResource(
            Payload operationInput,
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger)
        {
            logger.AddCloudEnvironment(cloudEnvironment)
                .FluentAddBaseValue(nameof(operationInput.CurrentState), operationInput.CurrentState);
        }

        private static void UpdateResourceStatus(
            EnvironmentTransition envRecord,
            ResourceAllocationRecord resourceRecord,
            StatusResponseBody resourceStatus,
            List<Guid> updatedResourceList)
        {
            if (resourceRecord.ResourceId != resourceStatus.ResourceId || resourceRecord.IsReady != resourceStatus.IsReady)
            {
                if (resourceRecord.ResourceId != resourceStatus.ResourceId)
                {
                    updatedResourceList.Add(resourceRecord.ResourceId);
                }

                envRecord.PushTransition((cloudEnvironment) =>
                {
                    var resource = resourceRecord.Type switch
                    {
                        ResourceType.ComputeVM => cloudEnvironment.Compute,
                        ResourceType.StorageFileShare => cloudEnvironment.Storage,
                        ResourceType.OSDisk => cloudEnvironment.OSDisk,
                        _ => throw new InvalidEnumArgumentException($"ResourceType {resourceRecord.Type} is not handled.")
                    };

                    if (resource.ResourceId != resourceStatus.ResourceId)
                    {
                        resource.ResourceId = resourceStatus.ResourceId;
                    }

                    if (resource.IsReady != resourceStatus.IsReady)
                    {
                        resource.IsReady = resourceStatus.IsReady;
                    }
                });
            }
        }

        /// <summary>
        /// Continuation input type.
        /// </summary>
        [JobPayload(JobPayloadNameOption.Name)]
        public class Payload : EntityContinuationJobPayloadBase<JobState>, IEnvironmentContinuationPayload
        {
            /// <inheritdoc/>
            public Guid EnvironmentId => EntityId;

            /// <summary>
            /// Gets or sets a value indicating whether the environment is created or not.
            /// </summary>
            public bool IsCreated { get; set; }

            /// <summary>
            /// Gets or sets the environment pool.
            /// </summary>
            public EnvironmentPool Pool { get; set; }
        }

        public enum JobState
        {
            /// <summary>
            /// Allocate resource.
            /// </summary>
            AllocateResource = 0,

            /// <summary>
            /// Check Resource State.
            /// </summary>
            CheckResourceState = 1,

            /// <summary>
            /// Kick off Environment Monitoring.
            /// </summary>
            StartHeartbeatMonitoring = 2,
        }

        public class CreateEnvironmentResourceResult : EntityContinuationResult
        {
        }
    }
}
