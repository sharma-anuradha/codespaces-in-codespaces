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
using Microsoft.Azure.Storage.Blob;
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
        private readonly IAzureClientFactory azureClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageFileShareProviderHelper"/> class.
        /// </summary>
        /// <param name="azureClientFactory">The azure client factory.</param>
        public StorageFileShareProviderHelper(
            IAzureClientFactory azureClientFactory)
        {
            this.azureClientFactory = Requires.NotNull(azureClientFactory, nameof(azureClientFactory));
        }

        /// <inheritdoc/>
        public Task<AzureResourceInfo> CreateStorageAccountAsync(
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

            return logger.OperationScopeAsync(
                "file_share_storage_provider_helper_connection_info",
                async (childLogger) =>
                {
                    var azure = await azureClientFactory.GetAzureClientAsync(new Guid(azureSubscriptionId));

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
                    var storageAccountName = await GenerateStorageAccountName(azure, childLogger);

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

                    childLogger.FluentAddValue("AzureStorageAccountName", storageAccountName)
                        .FluentAddValue("AzureStorageAccountRegion", azureRegion)
                        .FluentAddValue("AzureStorageAccountKind", storageCreateParams.Kind.ToString())
                        .FluentAddValue("AzureStorageAccountSkuName", storageCreateParams.Sku.Name.ToString());

                    await azure.StorageAccounts.Inner.CreateAsync(azureResourceGroup, storageAccountName, storageCreateParams);

                    return new AzureResourceInfo(Guid.Parse(azureSubscriptionId), azureResourceGroup, storageAccountName);
                });
        }

        /// <inheritdoc/>
        public Task CreateFileShareAsync(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            return logger.OperationScopeAsync(
                "file_share_storage_provider_helper_connection_info",
                async (childLogger) =>
                {
                    var cloudStorageAccount = await GetCloudStorageAccount(azureResourceInfo, null, childLogger);
                    var fileClient = cloudStorageAccount.CreateCloudFileClient();
                    var fileShare = fileClient.GetShareReference(StorageMountableShareName);
                    fileShare.Properties.Quota = StorageShareQuotaGb;
                    await fileShare.CreateIfNotExistsAsync();
                });
        }

        /// <inheritdoc/>
        public Task<ShareConnectionInfo> GetConnectionInfoAsync(
            AzureResourceInfo azureResourceInfo,
            StorageType storageType,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            return logger.OperationScopeAsync(
                "file_share_storage_provider_helper_get_connection_info",
                async (childLogger) =>
                {
                    var storageAccount = await GetStorageAccount(azureResourceInfo, childLogger);
                    var storageAccountName = storageAccount.Name;
                    var storageAccountKey = await GetStorageAccountKey(storageAccount);
                    var shareConnectionInfo = new ShareConnectionInfo(
                        storageAccountName,
                        storageAccountKey,
                        StorageMountableShareName,
                        GetStorageMountableFileName(storageType));
                    return shareConnectionInfo;
                });
        }

        /// <inheritdoc/>
        public Task<IStorageAccount> GetStorageAccount(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));

            return logger.OperationScopeAsync(
                "file_share_storage_provider_helper_get_storage_account",
                async (childLogger) =>
                {
                    childLogger.FluentAddValue("AzureStorageAccountName", azureResourceInfo.Name);

                    var azure = await azureClientFactory.GetAzureClientAsync(
                        azureResourceInfo.SubscriptionId);

                    return await azure.StorageAccounts.GetByResourceGroupAsync(
                        azureResourceInfo.ResourceGroup, azureResourceInfo.Name);
                });
        }

        /// <inheritdoc/>
        public Task<CloudStorageAccount> GetCloudStorageAccount(
            AzureResourceInfo azureResourceInfo,
            string storageAccountKey,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));

            return logger.OperationScopeAsync(
                "file_share_storage_provider_helper_get_cloud_storage_account",
                async (childLogger) =>
                {
                    childLogger.FluentAddValue("AzureStorageAccountName", azureResourceInfo.Name);

                    if (string.IsNullOrEmpty(storageAccountKey))
                    {
                        var storageAccount = await GetStorageAccount(azureResourceInfo, childLogger);
                        storageAccountKey = await GetStorageAccountKey(storageAccount);
                    }

                    var storageCreds = new StorageCredentials(azureResourceInfo.Name, storageAccountKey);
                    return new CloudStorageAccount(storageCreds, useHttps: true);
                });
        }

        /// <inheritdoc/>
        public Task DeleteStorageAccountAsync(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            return logger.OperationScopeAsync(
                "file_share_storage_provider_helper_delete_storage_account",
                async (childLogger) =>
                {
                    var azureSubscriptionId = azureResourceInfo.SubscriptionId;
                    var azure = await azureClientFactory.GetAzureClientAsync(azureSubscriptionId);

                    await azure.StorageAccounts.DeleteByResourceGroupAsync(azureResourceInfo.ResourceGroup, azureResourceInfo.Name);
                });
        }

        /// <inheritdoc/>
        public Task DeleteBlobAsync(
            AzureResourceInfo azureResourceInfo,
            string storageAccountKey,
            string blobContainerName,
            string blobName,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            return logger.OperationScopeAsync(
                "file_share_storage_provider_helper_delete_blob",
                async (childLogger) =>
                {
                    // Get the blob client for this storage account
                    var storageAccount = await GetCloudStorageAccount(azureResourceInfo, storageAccountKey, childLogger);
                    var blobClient = storageAccount.CreateCloudBlobClient();

                    // Get blob reference
                    var blobContainer = blobClient.GetContainerReference(blobContainerName);
                    var blob = blobContainer.GetBlockBlobReference(blobName);

                    await blob.DeleteIfExistsAsync();
                });
        }

        /// <inheritdoc/>
        public Task DeleteBlobContainerAsync(
            AzureResourceInfo azureResourceInfo,
            string storageAccountKey,
            string blobContainerName,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            return logger.OperationScopeAsync(
                "file_share_storage_provider_helper_delete_blob_container",
                async (childLogger) =>
                {
                    // Get the blob client for this storage account
                    var storageAccount = await GetCloudStorageAccount(azureResourceInfo, storageAccountKey, childLogger);
                    var blobClient = storageAccount.CreateCloudBlobClient();

                    // Get blob reference
                    var blobContainer = blobClient.GetContainerReference(blobContainerName);

                    await blobContainer.DeleteIfExistsAsync();
                });
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

        /// <inheritdoc/>
        public async Task<(CloudBlockBlob Blob, CloudBlobContainer BlobContainer)> FetchBlobAsync(
            AzureResourceInfo azureResourceInfo,
            string storageAccountKey,
            string blobContainerName,
            string blobName,
            IDiagnosticsLogger logger)
        {
            // Get the blob client for this storage account
            var storageAccount = await GetCloudStorageAccount(azureResourceInfo, storageAccountKey, logger);
            var blobClient = storageAccount.CreateCloudBlobClient();

            // Get blob reference
            var blobContainer = blobClient.GetContainerReference(blobContainerName);
            var blob = blobContainer.GetBlockBlobReference(blobName);

            // Make sure we create if it doesn't exist
            await blobContainer.CreateIfNotExistsAsync();

            return (blob, blobContainer);
        }

        /// <inheritdoc/>
        public (string Token, string BlobName, string BlobContainerName) FetchBlobSasToken(
            CloudBlockBlob blob,
            CloudBlobContainer blobContainer,
            SharedAccessBlobPermissions blobPermissions,
            IDiagnosticsLogger logger)
        {
            // Get blob sas token
            var blobUriWithSasToken = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                Permissions = blobPermissions,
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
            });
            var blobUriWithSas = $"{blobContainer.Uri}/{blob.Name}{blobUriWithSasToken}";

            return (blobUriWithSas, blob.Name, blobContainer.Name);
        }

        /// <inheritdoc/>
        public async Task<(string Token, string FileShareName, string FileName)> FetchStorageFileShareSasTokenAsync(
            AzureResourceInfo azureResourceInfo,
            string storageAccountKey,
            StorageType storageType,
            SharedAccessFilePermissions filePermissions,
            string filePrefix,
            IDiagnosticsLogger logger)
        {
            // Get file client for storage account
            var cloudStorageAccount = await GetCloudStorageAccount(
                azureResourceInfo, storageAccountKey, logger.NewChildLogger());
            var fileClient = cloudStorageAccount.CreateCloudFileClient();

            // Get file reference
            var fileShareName = GetStorageMountableShareName();
            var fileShare = fileClient.GetShareReference(fileShareName);
            var fileName = $"{filePrefix}{GetStorageMountableFileName(storageType)}";
            var fileReference = fileShare.GetRootDirectoryReference().GetFileReference(fileName);

            // Get file sas token
            var srcFileSas = fileShare.GetSharedAccessSignature(new SharedAccessFilePolicy()
            {
                Permissions = filePermissions,
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
            });
            var fileUriWithSas = fileReference.Uri.AbsoluteUri + srcFileSas;

            return (fileUriWithSas, fileShareName, fileName);
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
