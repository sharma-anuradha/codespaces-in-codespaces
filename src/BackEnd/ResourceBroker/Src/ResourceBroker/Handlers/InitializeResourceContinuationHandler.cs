// <copyright file="InitializeResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages initialization of a resource.
    /// </summary>
    public class InitializeResourceContinuationHandler
        : BaseContinuationTaskMessageHandler<InitializeResourceContinuationInput>, IInitializeResourceContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobInitializeResource";

        private const int VmInitializationRetryAfterSeconds = 30;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeResourceContinuationHandler"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="serviceProvider">Service provider.</param>
        public InitializeResourceContinuationHandler(
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider)
            : base(serviceProvider, resourceRepository)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerInitialize;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Initializing;

        /// <inheritdoc/>
        protected override Task<ContinuationInput> BuildOperationInputAsync(InitializeResourceContinuationInput input, ResourceRecordRef record, IDiagnosticsLogger logger)
        {
            if (record.Value.Type == Common.Contracts.ResourceType.ComputeVM)
            {
                var operationInput = new VirtualMachineProviderInitializeInput();
                return Task.FromResult<ContinuationInput>(operationInput);
            }
            else if (record.Value.Type == Common.Contracts.ResourceType.StorageFileShare)
            {
                var operationInput = new FileShareProviderInitializeInput();
                return Task.FromResult<ContinuationInput>(operationInput);
            }

            throw new NotSupportedException($"Resource type is not supported - {record.Value.Type}");
        }

        /// <inheritdoc/>
        protected override Task<ContinuationResult> RunOperationCoreAsync(InitializeResourceContinuationInput operationInput, ResourceRecordRef record, IDiagnosticsLogger logger)
        {
            ContinuationResult continuationResult;
            if (record.Value.Type == Common.Contracts.ResourceType.ComputeVM)
            {
                // VM is initialized when it is provisioned and heartbeat is received.
                if (record.Value.HeartBeatSummary != null &&
                    record.Value.HeartBeatSummary.LatestRawHeartBeat != null)
                {
                    continuationResult = new VirtualMachineProviderInitializeResult()
                    {
                        Status = OperationState.Succeeded,
                    };
                }
                else
                {
                    continuationResult = new VirtualMachineProviderInitializeResult()
                    {
                        Status = OperationState.InProgress,
                        RetryAfter = TimeSpan.FromSeconds(VmInitializationRetryAfterSeconds),
                    };
                }
            }
            else if (record.Value.Type == Common.Contracts.ResourceType.StorageFileShare)
            {
                // Nothing to do on storage. It is ready when provisioning completes.
                continuationResult = new FileShareProviderInitializeResult()
                {
                    Status = OperationState.Succeeded,
                };
            }
            else
            {
                throw new NotSupportedException($"Resource type is not supported - {record.Value.Type}");
            }

            return Task.FromResult(continuationResult);
        }
    }
}
