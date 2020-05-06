// <copyright file="DeleteOrphanedResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Delete Orphaned Resource Handler.
    /// </summary>
    public class DeleteOrphanedResourceContinuationHandler : IDeleteOrphanedResourceContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobDeleteOrphanedResource";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteOrphanedResourceContinuationHandler"/> class.
        /// </summary>
        /// <param name="computeProvider">Target compute provider.</param>
        /// <param name="storageProvider">Target storage provider.</param>
        /// <param name="diskProvider">Target disk provider.</param>
        /// <param name="keyVaultProvider">Target key vault provider.</param>
        /// <param name="resourceRepository">Resource repository.</param>
        public DeleteOrphanedResourceContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IDiskProvider diskProvider,
            IKeyVaultProvider keyVaultProvider,
            IResourceRepository resourceRepository)
        {
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
            DiskProvider = diskProvider;
            KeyVaultProvider = keyVaultProvider;
            ResourceRepository = resourceRepository;
        }

        private IComputeProvider ComputeProvider { get; set; }

        private IStorageProvider StorageProvider { get; set; }

        private IDiskProvider DiskProvider { get; set; }

        private IResourceRepository ResourceRepository { get; set; }

        private IKeyVaultProvider KeyVaultProvider { get; }

        private string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        public bool CanHandle(ContinuationQueuePayload payload)
        {
            return payload.Target == DefaultTarget;
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> Continue(ContinuationInput continuationInput, IDiagnosticsLogger logger)
        {
            var input = (DeleteOrphanedResourceContinuationInput)continuationInput;

            if (input == default)
            {
                return new ContinuationResult()
                {
                    ErrorReason = $"Incorrect continuation input provided to {nameof(DeleteOrphanedResourceContinuationHandler)}",
                    Status = OperationState.Failed,
                };
            }

            var result = default(ContinuationResult);
            if (input.Type == ResourceType.ComputeVM)
            {
                var keepOSDisk = await ShouldKeepOSDiskAsync(input.ResourceTags, logger);
                var computeOS = input.ResourceTags.GetComputeOS();

                var computeDeleteInput = new VirtualMachineProviderDeleteInput()
                {
                    AzureResourceInfo = input.AzureResourceInfo,
                    AzureVmLocation = input.AzureLocation,
                    PreserveOSDisk = keepOSDisk,
                    ComputeOS = computeOS,
                };

                result = await ComputeProvider.DeleteAsync(computeDeleteInput, logger.NewChildLogger());
            }
            else if (input.Type == ResourceType.KeyVault)
            {
                var keyvaultDeleteInput = new KeyVaultProviderDeleteInput()
                {
                    AzureResourceInfo = input.AzureResourceInfo,
                    AzureLocation = input.AzureLocation,
                };

                result = await KeyVaultProvider.DeleteAsync(keyvaultDeleteInput, logger.NewChildLogger());
            }
            else if (input.Type == ResourceType.OSDisk)
            {
                var osDiskDeleteInput = new DiskProviderDeleteInput()
                {
                    AzureResourceInfo = input.AzureResourceInfo,
                };

                result = await DiskProvider.DeleteDiskAsync(osDiskDeleteInput, logger.NewChildLogger());
            }
            else if (input.Type == ResourceType.StorageArchive || input.Type == ResourceType.StorageFileShare)
            {
                var storageDeleteInput = new FileShareProviderDeleteInput()
                {
                    AzureResourceInfo = input.AzureResourceInfo,
                };

                result = await StorageProvider.DeleteAsync(storageDeleteInput, logger.NewChildLogger());
            }
            else
            {
                throw new NotSupportedException($"Resource type is not supported - {input.Type}");
            }

            return result;
        }

        private async Task<bool> ShouldKeepOSDiskAsync(IDictionary<string, string> resourceTags, IDiagnosticsLogger logger)
        {
            return await resourceTags.HasBackingComponentRecordAsync(ResourceRepository, ResourceType.OSDisk, logger);
        }
    }
}
