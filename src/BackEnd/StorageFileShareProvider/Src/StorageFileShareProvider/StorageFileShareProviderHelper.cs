// <copyright file="StorageFileShareProviderHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
{
    /// <summary>
    /// Implements <see cref="IStorageFileShareProviderHelper"/> using Azure SDKs and APIs.
    /// </summary>
    public class StorageFileShareProviderHelper : IStorageFileShareProviderHelper
    {
        private const string StorageSkuNameStandard = "Standard_LRS";
        private const string StorageSkuNamePremium = "Premium_LRS";
        private static readonly int StorageAccountNameMaxLength = 24;
        private static readonly int StorageAccountNameGenerateMaxAttempts = 3;
        private static readonly int StorageShareQuotaGb = 100;
        private static readonly string StorageMountableShareName = "cloudenvdata";
        private static readonly string StorageAccountNamePrefix = "vsoce";
        private static readonly string StorageLinuxMountableFilename = "dockerlib";
        private static readonly string StorageWindowsMountableFilename = "windowsdisk.vhdx";
        private readonly ISystemCatalog systemCatalog;
        private readonly IBatchClientFactory batchClientFactory;
        private readonly StorageProviderSettings storageProviderSettings;
        private readonly IAzureClientFactory azureClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageFileShareProviderHelper"/> class.
        /// </summary>
        /// <param name="systemCatalog">System catalog.</param>
        /// <param name="batchClientFactory">Batch client factory.</param>
        /// <param name="storageProviderSettings">The storage provider settings.</param>
        public StorageFileShareProviderHelper(
            ISystemCatalog systemCatalog,
            IBatchClientFactory batchClientFactory,
            StorageProviderSettings storageProviderSettings)
        {
            this.systemCatalog = Requires.NotNull(systemCatalog, nameof(systemCatalog));
            this.batchClientFactory = Requires.NotNull(batchClientFactory, nameof(batchClientFactory));
            this.storageProviderSettings = Requires.NotNull(storageProviderSettings, nameof(storageProviderSettings));
            azureClientFactory = new AzureClientFactory(this.systemCatalog);
        }

        /// <inheritdoc/>
        public async Task<AzureResourceInfo> CreateStorageAccountAsync(
            string azureSubscriptionId,
            string azureRegion,
            string azureResourceGroup,
            string azureSkuName,
            IDictionary<string, string> resourceTags,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(azureRegion, nameof(azureRegion));
            Requires.NotNullOrEmpty(azureResourceGroup, nameof(azureResourceGroup));
            Requires.NotNullOrEmpty(azureSubscriptionId, nameof(azureSubscriptionId));
            Requires.NotNullOrEmpty(azureSkuName, nameof(azureSkuName));
            Requires.NotNull(resourceTags, nameof(resourceTags));

            logger = logger.WithValues(new LogValueSet
            {
                { "AzureRegion", azureRegion },
                { "AzureResourceGroup", azureResourceGroup },
                { "AzureSubscription", azureSubscriptionId },
            });

            var azure = await azureClientFactory.GetAzureClientAsync(new Guid(azureSubscriptionId));

            try
            {
                bool isPremiumSku;
                switch (azureSkuName)
                {
                    case StorageSkuNamePremium:
                        isPremiumSku = true;
                        break;
                    case StorageSkuNameStandard:
                        isPremiumSku = false;
                        break;
                    default:
                        throw new ArgumentException($"Unable to handle creation of storage account with sku of {azureSkuName}");
                }

                await azure.CreateResourceGroupIfNotExistsAsync(azureResourceGroup, azureRegion);
                var storageAccountName = await GenerateStorageAccountName(azure, logger);

                resourceTags.Add(ResourceTagName.ResourceName, storageAccountName);

                // Premium_LRS for Files requires a different kind of FileStorage
                // See https://docs.microsoft.com/en-us/azure/storage/common/storage-account-overview#types-of-storage-accounts
                var storageCreateParams = new StorageAccountCreateParameters()
                {
                    Location = azureRegion,
                    EnableHttpsTrafficOnly = true,
                    Tags = resourceTags,
                    Kind = isPremiumSku ? Kind.FileStorage : Kind.StorageV2,
                    Sku = new SkuInner(isPremiumSku ? SkuName.PremiumLRS : SkuName.StandardLRS),
                };

                logger.FluentAddValue("AzureStorageAccountName", storageAccountName)
                    .FluentAddValue("AzureStorageAccountRegion", azureRegion)
                    .FluentAddValue("AzureStorageAccountKind", storageCreateParams.Kind.ToString())
                    .FluentAddValue("AzureStorageAccountSkuName", storageCreateParams.Sku.Name.ToString());

                await azure.StorageAccounts.Inner.CreateAsync(azureResourceGroup, storageAccountName, storageCreateParams);

                logger.LogInfo("file_share_storage_provider_helper_create_storage_account_complete");

                return new AzureResourceInfo(Guid.Parse(azureSubscriptionId), azureResourceGroup, storageAccountName);
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_create_storage_account_error", ex);

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task CreateFileShareAsync(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            var azureSubscriptionId = azureResourceInfo.SubscriptionId;
            var azure = await azureClientFactory.GetAzureClientAsync(azureSubscriptionId);

            try
            {
                var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(azureResourceInfo.ResourceGroup, azureResourceInfo.Name);
                var storageAccountName = storageAccount.Name;
                var storageAccountKey = await GetStorageAccountKey(storageAccount);
                var storageCreds = new StorageCredentials(storageAccountName, storageAccountKey);
                var cloudStorageAccount = new CloudStorageAccount(storageCreds, useHttps: true);
                var fileClient = cloudStorageAccount.CreateCloudFileClient();
                var fileShare = fileClient.GetShareReference(StorageMountableShareName);
                fileShare.Properties.Quota = StorageShareQuotaGb;
                await fileShare.CreateIfNotExistsAsync();
                logger.LogInfo("file_share_storage_provider_helper_create_file_share_complete");
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_create_file_share_error", ex);
                throw ex;
            }
        }

        /// <inheritdoc/>
        public async Task<ShareConnectionInfo> GetConnectionInfoAsync(
            AzureResourceInfo azureResourceInfo,
            StorageType storageType,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            var azureSubscriptionId = azureResourceInfo.SubscriptionId;
            var azure = await azureClientFactory.GetAzureClientAsync(azureSubscriptionId);

            try
            {
                var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(azureResourceInfo.ResourceGroup, azureResourceInfo.Name);
                var storageAccountName = storageAccount.Name;
                var storageAccountKey = await GetStorageAccountKey(storageAccount);
                var shareConnectionInfo = new ShareConnectionInfo(
                    storageAccountName,
                    storageAccountKey,
                    StorageMountableShareName,
                    GetStorageMountableFileName(storageType));
                logger.LogInfo("file_share_storage_provider_helper_connection_info_complete");
                return shareConnectionInfo;
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_connection_info_error", ex);
                throw ex;
            }
        }

        /// <inheritdoc/>
        public async Task DeleteStorageAccountAsync(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            var azureSubscriptionId = azureResourceInfo.SubscriptionId;
            var azure = await azureClientFactory.GetAzureClientAsync(azureSubscriptionId);

            try
            {
                await azure.StorageAccounts.DeleteByResourceGroupAsync(azureResourceInfo.ResourceGroup, azureResourceInfo.Name);
                logger.LogInfo("file_share_storage_provider_helper_delete_storage_account_complete");
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_delete_storage_account_error", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetStorageAccountKey(IStorageAccount storageAccount)
        {
            var keys = await storageAccount.GetKeysAsync();
            var key1 = keys[0].Value;
            return key1;
        }

        /// <inheritdoc/>
        public string GetStorageMountableFileName(StorageType storageType)
        {
            return storageType == StorageType.Linux ? StorageLinuxMountableFilename : StorageWindowsMountableFilename;
        }

        /// <inheritdoc/>
        public string GetStorageMountableShareName()
        {
            return StorageMountableShareName;
        }

        private async Task<string> GenerateStorageAccountName(IAzure azure, IDiagnosticsLogger logger)
        {
            Requires.NotNull(azure, nameof(azure));

            var charsAvailable = StorageAccountNameMaxLength - StorageAccountNamePrefix.Length;

            for (var attempts = 1; attempts <= StorageAccountNameGenerateMaxAttempts; attempts++)
            {
                var accountGuid = Guid.NewGuid().ToString("N").Substring(0, charsAvailable);
                var storageAccountName = string.Concat(StorageAccountNamePrefix, accountGuid);

                var checkNameAvailabilityResult = await azure.StorageAccounts
                    .CheckNameAvailabilityAsync(storageAccountName);
                if (checkNameAvailabilityResult.IsAvailable == true)
                {
                    logger
                        .FluentAddValue("StorageAccountName", storageAccountName)
                        .FluentAddValue("AttemptsTaken", attempts.ToString())
                        .LogInfo("file_share_storage_provider_helper_generate_account_name_complete");
                    return storageAccountName;
                }
            }

            logger.LogError("file_share_storage_provider_helper_generate_account_name_error");
            throw new StorageCreateException("Unable to generate storage account name");
        }
    }
}
