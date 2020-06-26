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
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Delete Orphaned Resource Handler.
    /// Note: This handler does not follow the same model of other handles by inheriting from the base handler because,
    /// the base handler will look up the records, update them on every continue. But in the orphaned resource cleanup,
    /// the records don't exist for orphaned resources. Hence the simpler model.
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
            if (continuationInput is DeleteOrphanedResourceContinuationInput deleteOrphanedResourceInput)
            {
                return await CreateDeleteContinuationAsync(deleteOrphanedResourceInput, logger);
            }
            else
            {
                return await ContinueDeleteAsync(continuationInput, logger);
            }
        }

        private async Task<ContinuationResult> ContinueDeleteAsync(ContinuationInput continuationInput, IDiagnosticsLogger logger)
        {
            var result = default(ContinuationResult);

            if (continuationInput is VirtualMachineProviderDeleteInput computeInput)
            {
                result = await ComputeProvider.DeleteAsync(computeInput, logger.WithValues(new LogValueSet()));
            }
            else if (continuationInput is FileShareProviderDeleteInput storageInput)
            {
                result = await StorageProvider.DeleteAsync(storageInput, logger.WithValues(new LogValueSet()));
            }
            else if (continuationInput is DiskProviderDeleteInput diskInput)
            {
                result = await DiskProvider.DeleteDiskAsync(diskInput, logger.WithValues(new LogValueSet()));
            }
            else if (continuationInput is KeyVaultProviderDeleteInput keyVaultInput)
            {
                result = await KeyVaultProvider.DeleteAsync(keyVaultInput, logger.WithValues(new LogValueSet()));
            }
            else
            {
                throw new NotSupportedException($"Continuation type is not supported - {continuationInput.GetType().Name}");
            }

            return result;
        }

        private async Task<ContinuationResult> CreateDeleteContinuationAsync(DeleteOrphanedResourceContinuationInput deleteOrphanedResourceInput, IDiagnosticsLogger logger)
        {
            var result = default(ContinuationResult);

            if (deleteOrphanedResourceInput.Type == ResourceType.ComputeVM)
            {
                // TODO:: Handle Network Interface deletion by keeping the compute record unless deletion complets successfully.
                // or add resource record for Network Interface.
                var osDiskResourceInfo = await GetBackingOSDiskResourceComponentAsync(deleteOrphanedResourceInput.ResourceTags, logger);
                var computeOS = deleteOrphanedResourceInput.ResourceTags.GetComputeOS();
                var customComponents = new List<ResourceComponent>() { osDiskResourceInfo };

                var computeDeleteInput = new VirtualMachineProviderDeleteInput()
                {
                    AzureResourceInfo = deleteOrphanedResourceInput.AzureResourceInfo,
                    CustomComponents = customComponents,
                    AzureVmLocation = deleteOrphanedResourceInput.AzureLocation,
                    ComputeOS = computeOS,
                };

                result = await ComputeProvider.DeleteAsync(computeDeleteInput, logger.NewChildLogger());
            }
            else if (deleteOrphanedResourceInput.Type == ResourceType.KeyVault)
            {
                var keyvaultDeleteInput = new KeyVaultProviderDeleteInput()
                {
                    AzureResourceInfo = deleteOrphanedResourceInput.AzureResourceInfo,
                    AzureLocation = deleteOrphanedResourceInput.AzureLocation,
                };

                result = await KeyVaultProvider.DeleteAsync(keyvaultDeleteInput, logger.NewChildLogger());
            }
            else if (deleteOrphanedResourceInput.Type == ResourceType.OSDisk)
            {
                var osDiskDeleteInput = new DiskProviderDeleteInput()
                {
                    AzureResourceInfo = deleteOrphanedResourceInput.AzureResourceInfo,
                };

                result = await DiskProvider.DeleteDiskAsync(osDiskDeleteInput, logger.NewChildLogger());
            }
            else if (deleteOrphanedResourceInput.Type == ResourceType.StorageArchive || deleteOrphanedResourceInput.Type == ResourceType.StorageFileShare)
            {
                var storageDeleteInput = new FileShareProviderDeleteInput()
                {
                    AzureResourceInfo = deleteOrphanedResourceInput.AzureResourceInfo,
                };

                result = await StorageProvider.DeleteAsync(storageDeleteInput, logger.NewChildLogger());
            }
            else
            {
                throw new NotSupportedException($"Resource type is not supported - {deleteOrphanedResourceInput.Type}");
            }

            return result;
        }

        private async Task<ResourceComponent> GetBackingOSDiskResourceComponentAsync(IDictionary<string, string> resourceTags, IDiagnosticsLogger logger)
        {
            return await resourceTags.GetBackingComponentRecordAsync(ResourceRepository, ResourceType.OSDisk, logger);
        }
    }
}
