// <copyright file="CreateEnvironmentResourceJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
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
            if (operationInput.ComputeResource != default)
            {
                resourceList.Add(operationInput.ComputeResource.ResourceId);
            }

            if (operationInput.StorageResource != default)
            {
                resourceList.Add(operationInput.StorageResource.ResourceId);
            }

            if (record.Value.OSDisk != default)
            {
                resourceList.Add(operationInput.OSDiskResource.ResourceId);
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
        protected override async Task<ContinuationJobResult<JobState, CreateEnvironmentResourceJobHandler.CreateEnvironmentResourceResult>> ContinueAsync(
            Payload payload,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken)
        {
            // Add environment id and resource ids to logger
            LogResource(payload, logger);

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

        private async Task<ContinuationJobResult<JobState, CreateEnvironmentResourceJobHandler.CreateEnvironmentResourceResult>> RunAllocateResourceAsync(
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

            // Setup result
            operationInput.ComputeResource = computeResponse.BuildQueueInputResource();
            operationInput.OSDiskResource = osDiskResponse.BuildQueueInputResource();
            operationInput.StorageResource = storageResponse.BuildQueueInputResource();

            bool didUpdate = await UpdateResourceInfoAsync(operationInput, record, logger);
            if (!didUpdate)
            {
                return ReturnFailed(ResultFromReason("FailedToUpdateEnvironmentRecord"));
            }

            LogResource(operationInput, logger);

            return ReturnNextState(JobState.CheckResourceState);
        }

        private async Task<ContinuationJobResult<JobState, CreateEnvironmentResourceJobHandler.CreateEnvironmentResourceResult>> RunCheckResourceStateAsync(
            Payload operationInput,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger)
        {
            var hasStorageResource = operationInput.StorageResource != default;
            var hasOSDiskResource = operationInput.OSDiskResource != default;

            var resourceList = new List<Guid>() { operationInput.ComputeResource.ResourceId };
            if (hasStorageResource)
            {
                resourceList.Add(operationInput.StorageResource.ResourceId);
            }

            if (hasOSDiskResource)
            {
                resourceList.Add(operationInput.OSDiskResource.ResourceId);
            }

            var statusResponse = await ResourceBrokerHttpClient.StatusAsync(
                operationInput.EnvironmentId,
                resourceList,
                logger.NewChildLogger());

            var computeStatus = statusResponse.Single(x => x.Type == ResourceType.ComputeVM);
            var osDiskStatus = statusResponse.SingleOrDefault(x => x.Type == ResourceType.OSDisk);
            var storageStatus = statusResponse.SingleOrDefault(x => x.Type == ResourceType.StorageFileShare);

            var updatedResourceList = new List<Guid>();
            operationInput.ComputeResource = UpdateResourceStatus(computeStatus, operationInput.ComputeResource, updatedResourceList);

            if (hasOSDiskResource)
            {
                operationInput.OSDiskResource = UpdateResourceStatus(osDiskStatus, operationInput.OSDiskResource, updatedResourceList);
            }

            if (hasStorageResource)
            {
                operationInput.StorageResource = UpdateResourceStatus(storageStatus, operationInput.StorageResource, updatedResourceList);
            }

            LogResource(operationInput, logger);

            bool resourcesReady = statusResponse.All(status => status.IsReady);

            if (updatedResourceList.Count != 0)
            {
                try
                {
                    await ResourceBrokerHttpClient.DeleteAsync(Guid.Parse(record.Value.Id), updatedResourceList, logger.NewChildLogger());
                }
                catch (Exception ex)
                {
                    // Continue on failure to delete shadow record, as it is best effort.
                    logger.LogException($"{LogBaseName}_delete_shadow_record_error", ex);
                }
            }

            if (resourcesReady || updatedResourceList.Count != 0)
            {
                // Queued allocation request is completed, so update resource information in environment record.
                var didUpdate = await UpdateResourceInfoAsync(operationInput, record, logger.NewChildLogger());
                if (!didUpdate)
                {
                    // retry to update the updated resource in environment record.
                    return ReturnRetry(TimeSpan.FromSeconds(1));
                }
            }

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

        public async Task<ContinuationJobResult<JobState, CreateEnvironmentResourceJobHandler.CreateEnvironmentResourceResult>> RunStartEnvironmentMonitoring(
            Payload operationInput,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger)
        {
            // TODO:: Kick off heartbeat monitoring, once : https://github.com/microsoft/vssaas-planning/issues/1051, is done

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
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ComputeResourceId", operationInput.ComputeResource?.ResourceId)
                .FluentAddBaseValue("ComputeResourceReady", operationInput.ComputeResource?.IsReady)
                .FluentAddBaseValue("StorageResourceId", operationInput.StorageResource?.ResourceId)
                .FluentAddBaseValue("StorageResourceReady", operationInput.StorageResource?.IsReady)
                .FluentAddBaseValue("OSDiskResourceId", operationInput.OSDiskResource?.ResourceId)
                .FluentAddBaseValue("OSDisResourceReady", operationInput.OSDiskResource?.IsReady)
                .AddBaseEnvironmentId(operationInput.EnvironmentId)
                .FluentAddBaseValue(nameof(operationInput.CurrentState), operationInput.CurrentState);
        }

        private async Task<bool> UpdateResourceInfoAsync(
           Payload operationInput,
           IEntityRecordRef<CloudEnvironment> record,
           IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_update_resources_post_allocate",
                async (childLogger) =>
                {
                    var hasStorageResource = operationInput.StorageResource != default;
                    var hasOSDiskResource = operationInput.OSDiskResource != default;

                    var computeResource = operationInput.ComputeResource.BuildResourceRecord();
                    var osDiskResource = operationInput.OSDiskResource.BuildResourceRecord();
                    var storageResource = operationInput.StorageResource.BuildResourceRecord();

                    return await CloudEnvironmentRepository.UpdateRecordAsync(
                        operationInput.EnvironmentId,
                        record,
                        (environment, innerLogger) =>
                        {
                            // Update compute and disk resources
                            record.Value.Compute = computeResource;
                            if (hasOSDiskResource)
                            {
                                record.Value.OSDisk = osDiskResource;
                                record.Value.OSDiskSnapshot = null;
                            }

                            // For archived environments, dont switch storage resource.
                            if (hasStorageResource)
                            {
                                record.Value.Storage = storageResource;
                            }

                            return Task.FromResult(true);
                        },
                        logger,
                        LogBaseName);
                });
        }

        private static EnvironmentContinuationInputResource UpdateResourceStatus(
            StatusResponseBody resourceStatus,
            EnvironmentContinuationInputResource inputResource,
            List<Guid> shadowResourceList)
        {
            if (inputResource.ResourceId != resourceStatus.ResourceId)
            {
                shadowResourceList.Add(inputResource.ResourceId);
            }

            inputResource = resourceStatus.BuildQueueInputResource();

            return inputResource;
        }

        /// <summary>
        /// Continuation input type.
        /// </summary>
        [JobPayload(JobPayloadNameOption.Name)]
        public class Payload : EntityContinuationJobPayloadBase<JobState>, IEnvironmentContinuationPayload
        {
            /// <inheritdoc/>
            public Guid EnvironmentId => EntityId;

            public bool IsCreated { get; set; }

            /// <summary>
            /// Gets or sets the environment pool.
            /// </summary>
            public EnvironmentPool Pool { get; set; }

            /// <summary>
            /// Gets or sets the compute resource for environment.
            /// </summary>
            public EnvironmentContinuationInputResource ComputeResource { get; set; }

            /// <summary>
            /// Gets or sets the osdisk resource for environment.
            /// </summary>
            public EnvironmentContinuationInputResource OSDiskResource { get; set; }

            /// <summary>
            /// Gets or sets the storage resource for environment.
            /// </summary>
            public EnvironmentContinuationInputResource StorageResource { get; set; }
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
