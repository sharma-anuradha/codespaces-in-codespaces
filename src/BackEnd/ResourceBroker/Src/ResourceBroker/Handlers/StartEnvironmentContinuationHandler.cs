// <copyright file="StartEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
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
        public const string DefaultQueueTarget = "JobStartCompute";

        /// <summary>
        /// Initializes a new instance of the <see cref="StartEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        public StartEnvironmentContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository)
            : base(resourceRepository)
        {
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
        }

        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Starting;

        private IComputeProvider ComputeProvider { get; }

        private IStorageProvider StorageProvider { get; }

        /// <inheritdoc/>
        protected override async Task<ContinuationInput> BuildOperationInputAsync(StartEnvironmentContinuationInput input, ResourceRecordRef compute, IDiagnosticsLogger logger)
        {
            var storageResult = await AssignStorageAsync(input.StorageResourceId, logger);
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

        private async Task<FileShareProviderAssignResult> AssignStorageAsync(Guid storageId, IDiagnosticsLogger logger)
        {
            // Fetch storage reference
            var storage = await FetchReferenceAsync(storageId, logger);

            // Update storage to be inprogress
            await SaveStatusAsync(storage, OperationState.Initialized, logger);

            // Get file share connection info for target share
            var fileShareProviderAssignInput = new FileShareProviderAssignInput
            {
                AzureResourceInfo = storage.Value.AzureResourceInfo,
            };
            var storageResult = await StorageProvider.AssignAsync(fileShareProviderAssignInput, logger);

            // Update storage to be completed
            await SaveStatusAsync(storage, storageResult.Status, logger);

            return storageResult;
        }
    }
}
