// <copyright file="CleanupResourceContinuationJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation job handler that manages starting of environment.
    /// </summary>
    public class CleanupResourceContinuationJobHandler
        : ResourceContinuationJobHandlerBase<CleanupResourceContinuationJobHandler.Payload, EmptyContinuationState, EntityContinuationResult>
    {
        /// <summary>
        /// Gets default queue id for item on queue.
        /// </summary>
        public const string DefaultQueueId = "jobhandler-cleanup-resource";

        /// <summary>
        /// Initializes a new instance of the <see cref="CleanupResourceContinuationJobHandler"/> class.
        /// </summary>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="queueProvider">Queue provider.</param>
        /// <param name="resourceStateManager">Request state Manager to update resource state.</param>
        /// <param name="jobQueueProducerFactory">Job queue producer factory</param>
        public CleanupResourceContinuationJobHandler(
            IComputeProvider computeProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider,
            IQueueProvider queueProvider,
            IResourceStateManager resourceStateManager,
            IJobQueueProducerFactory jobQueueProducerFactory)
            : base(serviceProvider, resourceRepository, resourceStateManager, jobQueueProducerFactory)
        {
            ComputeProvider = computeProvider;
            QueueProvider = queueProvider;
        }

        /// <inheritdoc/>
        public override string QueueId => DefaultQueueId;

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerCleanup;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.CleanUp;

        private IComputeProvider ComputeProvider { get; set; }

        private IQueueProvider QueueProvider { get; set; }

        /// <inheritdoc/>
        protected override async Task<ContinuationJobResult<EmptyContinuationState, EntityContinuationResult>> ContinueAsync(Payload payload, IEntityRecordRef<ResourceRecord> record, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var result = (ContinuationResult)null;

            if (record.Value.Type != ResourceType.ComputeVM)
            {
                throw new NotSupportedException($"Resource type is not supported - {record.Value.Type}");
            }

            if (payload.VirtualMachineShutdownInput == null)
            {
                var didParseLocation = Enum.TryParse(record.Value.Location, true, out AzureLocation azureLocation);
                if (!didParseLocation)
                {
                    throw new NotSupportedException($"Provided location of '{record.Value.Location}' is not supported.");
                }

                var keepDisk = record.Value.GetComputeDetails().OSDiskRecordId != default;

                payload.VirtualMachineShutdownInput = new VirtualMachineProviderShutdownInput
                {
                    AzureResourceInfo = record.Value.AzureResourceInfo,
                    AzureVmLocation = azureLocation,
                    ComputeOS = record.Value.PoolReference.GetComputeOS(),
                    EnvironmentId = payload.EnvironmentId.ToString(),
                    PreserveOSDisk = keepDisk,
                };           
            }

            result = await ShutDownResourceAsync(payload.VirtualMachineShutdownInput, record, logger);
            if (result.NextInput != null)
            {
                payload.VirtualMachineShutdownInput = (VirtualMachineProviderShutdownInput)result.NextInput;
            }

            return ToContinuationInfo(result, payload);
        }

        private async Task<ContinuationResult> ShutDownResourceAsync(VirtualMachineProviderShutdownInput input, IEntityRecordRef<ResourceRecord> record, IDiagnosticsLogger logger)
        {
            // Handle compute case only, don't need to do anything with storage here as its already circuited.
            if (record.Value.Type == ResourceType.ComputeVM)
            {
                var queueComponent = record.Value.Components?.Items?.SingleOrDefault(x => x.Value.ComponentType == ResourceType.InputQueue).Value;
                if (queueComponent == default)
                {
                    return await ComputeProvider.ShutdownAsync(input, logger.NewChildLogger());
                }
                else
                {
                    var queueMessage = input.GenerateShutdownEnvironmentPayload();
                    var result = await QueueProvider.PushMessageAsync(
                        queueComponent.AzureResourceInfo,
                        queueMessage,
                        logger);

                    return new VirtualMachineProviderShutdownResult()
                    {
                        Status = result.Status,
                        ErrorReason = result.ErrorReason,
                    };
                }
            }

            throw new NotSupportedException($"Resource type is not supported - {record.Value.Type}");
        }

        [JobPayload(JobPayloadNameOption.Name)]
        public class Payload : EntityContinuationJobPayloadBase<EmptyContinuationState>
        {
            /// <summary>
            /// Gets or sets the environment id.
            /// </summary>
            public Guid EnvironmentId { get; set; }

            public VirtualMachineProviderShutdownInput VirtualMachineShutdownInput { get; set; }
        }
    }
}
