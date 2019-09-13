// <copyright file="StartEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages starting of environement.
    /// </summary>
    public class StartEnvironmentContinuationHandler
        : BaseContinuationTaskMessageHandler<StartEnvironmentContinuationInput>, IStartEnvironmentContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobStartEnvironment";

        /// <summary>
        /// Initializes a new instance of the <see cref="StartEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="serviceProvider">Service Provider.</param>
        public StartEnvironmentContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider)
            : base(serviceProvider, resourceRepository)
        {
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerStart;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Starting;

        private IComputeProvider ComputeProvider { get; }

        private IStorageProvider StorageProvider { get; }

        /// <inheritdoc/>
        protected override async Task<ContinuationInput> BuildOperationInputAsync(StartEnvironmentContinuationInput input, ResourceRecordRef compute, IDiagnosticsLogger logger)
        {
            var storageResult = await AssignStorageAsync(input, input.StorageResourceId, logger);
            if (storageResult.Status != OperationState.Succeeded)
            {
                return null;
            }

            return new VirtualMachineProviderStartComputeInput(
                compute.Value.AzureResourceInfo,
                new ShareConnectionInfo(
                    storageResult.StorageAccountName,
                    storageResult.StorageAccountKey,
                    storageResult.StorageShareName,
                    storageResult.StorageFileName),
                input.EnvironmentVariables,
                null);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationAsync(ContinuationInput operationInput, ResourceRecordRef compute, IDiagnosticsLogger logger)
        {
            return await ComputeProvider.StartComputeAsync((VirtualMachineProviderStartComputeInput)operationInput, logger.WithValues(new LogValueSet()));
        }

        private async Task<FileShareProviderAssignResult> AssignStorageAsync(StartEnvironmentContinuationInput input, Guid storageId, IDiagnosticsLogger logger)
        {
            // Fetch storage reference
            var storage = await FetchReferenceAsync(storageId, logger);

            // Update storage to be inprogress
            await SaveStatusAsync(input, storage, OperationState.Initialized, "PreAssignStorage", logger);

            // Get file share connection info for target share
            var fileShareProviderAssignInput = new FileShareProviderAssignInput
            {
                AzureResourceInfo = storage.Value.AzureResourceInfo,
            };
            var storageResult = await StorageProvider.AssignAsync(fileShareProviderAssignInput, logger);

            // Update storage to be completed
            await SaveStatusAsync(input, storage, storageResult.Status, "PostAssignStorage", logger);

            return storageResult;
        }
    }
}
