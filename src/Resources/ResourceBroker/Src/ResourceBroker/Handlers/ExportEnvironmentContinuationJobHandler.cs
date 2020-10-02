// <copyright file="ExportEnvironmentContinuationJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
using QueueMessage = Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts.QueueMessage;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
#pragma warning disable CA1501
    /// <summary>
    /// Continuation handler that manages exporting of environment.
    /// </summary>
    public class ExportEnvironmentContinuationJobHandler
        : StartEnvironmentContinuationJobHandlerBase<ExportEnvironmentContinuationJobHandler.Payload, ExportEnvironmentContinuationJobHandler.ExportEnvironmentContinuationResult>
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueId = "jobhandler-export-environment";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResumeEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="storageFileShareProviderHelper">Storage File Share Provider Helper.</param>
        /// <param name="queueProvider">Queue provider.</param>
        /// <param name="resourceStateManager">Resource state manager.</param>
        /// <param name="exportStorageProvider">Export storage provider.</param>
        /// <param name="jobQueueProducerFactory">A job queue producer factory.</param>
        public ExportEnvironmentContinuationJobHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IStorageFileShareProviderHelper storageFileShareProviderHelper,
            IQueueProvider queueProvider,
            IExportStorageProvider exportStorageProvider,
            IResourceStateManager resourceStateManager,
            IJobQueueProducerFactory jobQueueProducerFactory)
            : base(computeProvider, storageProvider, resourceRepository, storageFileShareProviderHelper, queueProvider, resourceStateManager, jobQueueProducerFactory)
        {
            ExportStorageProvider = exportStorageProvider;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerExportEnvironment;

        /// <inheritdoc/>
        public override string QueueId => DefaultQueueId;

        private IExportStorageProvider ExportStorageProvider { get; }

        protected override async Task<VirtualMachineProviderStartComputeInput> CreateStartComputeInputAsync(Payload input, IEntityRecordRef<ResourceRecord> record, IDiagnosticsLogger logger)
        {
            // Adding export storage info
            if (input.EnvironmentVariables.ContainsKey("storageExportAccountSasToken") == false)
            {
                await SetupExportStorageInfoAsync(input, logger);
            }

            return await base.CreateStartComputeInputAsync(input, record, logger);
        }

        /// <inheritdoc/>
        protected override VirtualMachineProviderStartComputeInput CreateStartComputeInput(Payload input, IEntityRecordRef<ResourceRecord> compute, ShareConnectionInfo shareConnectionInfo, ComputeOS computeOs, AzureLocation azureLocation)
        {
            return new VirtualMachineProviderStartComputeInput(
                compute.Value.AzureResourceInfo,
                shareConnectionInfo,
                input.EnvironmentVariables,
                input.UserSecrets,
                null,
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

        private async Task SetupExportStorageInfoAsync(Payload input, IDiagnosticsLogger logger)
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

        [JobPayload(JobPayloadNameOption.Name)]
        public class Payload : StartEnvironmentContinuationPayloadBase
        {
        }

        public class ExportEnvironmentContinuationResult : EntityContinuationResult
        {
        }
    }
#pragma warning restore CA1501
}
