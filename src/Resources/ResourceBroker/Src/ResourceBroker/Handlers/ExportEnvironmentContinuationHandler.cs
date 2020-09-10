// <copyright file="ExportEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages exporting of environment.
    /// </summary>
    public class ExportEnvironmentContinuationHandler
        : BaseStartEnvironmentContinuationHandler<StartExportContinuationInput>, IExportEnvironmentContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobExportEnvironment";

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="serviceProvider">Service Provider.</param>
        /// <param name="storageFileShareProviderHelper">Storage File Share Provider Helper.</param>
        /// <param name="queueProvider">Queue provider.</param>
        /// <param name="exportStorageProvider">Export storage provider.</param>
        /// <param name="resourceStateManager">Resource state manager.</param>
        public ExportEnvironmentContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider,
            IStorageFileShareProviderHelper storageFileShareProviderHelper,
            IQueueProvider queueProvider,
            IExportStorageProvider exportStorageProvider,
            IResourceStateManager resourceStateManager)
             : base(computeProvider, storageProvider, resourceRepository, serviceProvider, storageFileShareProviderHelper, queueProvider, resourceStateManager)
        {
            ExportStorageProvider = exportStorageProvider;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerExportEnvironment;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        private IExportStorageProvider ExportStorageProvider { get; }

        /// <inheritdoc/>
        protected override async Task<ContinuationInput> BuildOperationInputAsync(StartExportContinuationInput input, ResourceRecordRef compute, IDiagnosticsLogger logger)
        {
            // Adding export storage info
            if (input.EnvironmentVariables.ContainsKey("storageExportAccountSasToken") == false)
            {
                await SetupExportStorageInfo(input, logger);
            }

            return await ConfigureBuildOperationInputAsync(input, compute, logger);
        }

        /// <inheritdoc/>
        protected override Task<ContinuationResult> RunOperationCoreAsync(StartExportContinuationInput input, ResourceRecordRef compute, IDiagnosticsLogger logger)
        {
            return ConfigureRunOperationCoreAsync(input, compute, logger);
        }

        /// <inheritdoc/>
        protected override VirtualMachineProviderStartComputeInput CreateStartComputeInput(StartExportContinuationInput input, ResourceRecordRef compute, ShareConnectionInfo shareConnectionInfo, ComputeOS computeOs, AzureLocation azureLocation)
        {
            return new VirtualMachineProviderStartComputeInput(
                   compute.Value.AzureResourceInfo,
                   shareConnectionInfo,
                   input.EnvironmentVariables,
                   input.UserSecrets,
                   computeOs,
                   azureLocation,
                   compute.Value.SkuName,
                   null);
        }

        /// <inheritdoc/>
        protected override QueueMessage GeneratePayload(VirtualMachineProviderStartComputeInput startComputeInput)
        {
            return startComputeInput.GenerateExportEnvironmentPayload();
        }

        private async Task SetupExportStorageInfo(StartExportContinuationInput input, IDiagnosticsLogger logger)
        {
            var fileShareReference = await FetchReferenceAsync(input.StorageResourceId.Value, logger);
            var fileShareRecordDetails = fileShareReference.Value.GetStorageDetails();

            // Get export blob details
            var exportStorageInfo = await ExportStorageProvider.GetExportStorageAccountAsync(
                fileShareRecordDetails.Location, fileShareRecordDetails.SizeInGB, logger.NewChildLogger());

            // Creates SAS token for read/write of export blob
            var exportBlob = await StorageFileShareProviderHelper.FetchExportBlobSasTokenAsync(
                exportStorageInfo.AzureResourceInfo,
                input.EnvironmentId.ToString(),
                exportStorageInfo.StorageAccountKey,
                SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write,
                logger.NewChildLogger());

            // Creates SAS token for read of export blob
            var exportReadBlob = await StorageFileShareProviderHelper.FetchExportBlobSasTokenAsync(
                exportStorageInfo.AzureResourceInfo,
                input.EnvironmentId.ToString(),
                exportStorageInfo.StorageAccountKey,
                SharedAccessBlobPermissions.Read,
                logger.NewChildLogger());

            input.EnvironmentVariables.Add("storageExportAccountSasToken", exportBlob.Token);
            input.EnvironmentVariables.Add("storageExportReadAccountSasToken", exportReadBlob.Token);
        }
    }
}
