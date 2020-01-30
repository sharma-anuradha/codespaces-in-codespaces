﻿// <copyright file="StartEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
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
    /// Continuation handler that manages starting of environment.
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
            var computeOs = compute.Value.PoolReference.GetComputeOS();

            var storageResult = await AssignStorageAsync(input, input.StorageResourceId, computeOs, logger);
            if (storageResult.Status != OperationState.Succeeded)
            {
                return null;
            }

            var didParseLocation = Enum.TryParse(compute.Value.Location, true, out AzureLocation azureLocation);
            if (!didParseLocation)
            {
                throw new NotSupportedException($"Provided location of '{compute.Value.Location}' is not supported.");
            }

            return new VirtualMachineProviderStartComputeInput(
                compute.Value.AzureResourceInfo,
                new ShareConnectionInfo(
                    storageResult.StorageAccountName,
                    storageResult.StorageAccountKey,
                    storageResult.StorageShareName,
                    storageResult.StorageFileName),
                input.EnvironmentVariables,
                computeOs,
                azureLocation,
                compute.Value.SkuName,
                null);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(StartEnvironmentContinuationInput input, ResourceRecordRef compute, IDiagnosticsLogger logger)
        {
            return await ComputeProvider.StartComputeAsync((VirtualMachineProviderStartComputeInput)input.OperationInput, logger.WithValues(new LogValueSet()));
        }

        private async Task<FileShareProviderAssignResult> AssignStorageAsync(StartEnvironmentContinuationInput input, Guid storageId, ComputeOS computeOS, IDiagnosticsLogger logger)
        {
            // Fetch storage reference
            var storage = await FetchReferenceAsync(storageId, logger);

            // Update storage to be inprogress
            await UpdateRecordStatusAsync(input, storage, OperationState.Initialized, "PreAssignStorage", logger);

            // Get file share connection info for target share
            var fileShareProviderAssignInput = new FileShareProviderAssignInput
            {
                AzureResourceInfo = storage.Value.AzureResourceInfo,
                StorageType = computeOS == ComputeOS.Windows ? StorageType.Windows : StorageType.Linux,
            };
            var storageResult = await StorageProvider.AssignAsync(fileShareProviderAssignInput, logger);

            // Update storage to be completed
            await UpdateRecordStatusAsync(input, storage, storageResult.Status, "PostAssignStorage", logger);

            return storageResult;
        }
    }
}
