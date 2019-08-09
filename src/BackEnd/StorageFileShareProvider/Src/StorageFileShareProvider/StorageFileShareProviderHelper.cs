// <copyright file="StorageFileShareProviderHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
{
    /// <summary>
    /// Implements <see cref="IStorageFileShareProviderHelper"/> using Azure SDKs and APIs.
    /// </summary>
    public class StorageFileShareProviderHelper : IStorageFileShareProviderHelper
    {
        private static readonly int StorageAccountNameMaxLength = 24;
        private static readonly int StorageAccountNameGenerateMaxAttempts = 3;
        private static readonly string StorageMountableShareName = "cloudenvdata";
        private static readonly string StorageMountableFileName = "dockerlib";
        private static readonly string StorageAccountNamePrefix = "vsoce";
        private readonly ISystemCatalog systemCatalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageFileShareProviderHelper"/> class.
        /// </summary>
        /// <param name="systemCatalog">System catalog.</param>
        public StorageFileShareProviderHelper(ISystemCatalog systemCatalog)
        {
            this.systemCatalog = Requires.NotNull(systemCatalog, nameof(systemCatalog));
        }

        /// <inheritdoc/>
        public async Task<string> CreateStorageAccountAsync(
            string azureSubscriptionId,
            string azureRegion,
            string azureResourceGroup)
        {
            Requires.NotNullOrEmpty(azureRegion, nameof(azureRegion));
            Requires.NotNullOrEmpty(azureResourceGroup, nameof(azureResourceGroup));
            Requires.NotNullOrEmpty(azureSubscriptionId, nameof(azureSubscriptionId));

            var azure = await GetAzureClient(azureSubscriptionId);
            var storageAccountName = await GenerateStorageAccountName(azure);
            var storageAccount = await azure.StorageAccounts.Define(storageAccountName)
                .WithRegion(azureRegion)
                .WithExistingResourceGroup(azureResourceGroup)
                .WithGeneralPurposeAccountKindV2()
                .WithOnlyHttpsTraffic()
                .WithSku(Azure.Management.Storage.Fluent.StorageAccountSkuType.Standard_LRS)
                .CreateAsync();
            return storageAccount.Id;
        }

        /// <inheritdoc/>
        public async Task CreateFileShareAsync(string azureStorageAccountId)
        {
            Requires.NotNullOrEmpty(azureStorageAccountId, nameof(azureStorageAccountId));

            var azureSubscriptionId = GetAzureSubscriptionIdFromResourceId(azureStorageAccountId);

            var azure = await GetAzureClient(azureSubscriptionId);
            var storageAccount = await azure.StorageAccounts.GetByIdAsync(azureStorageAccountId);
            var storageAccountName = storageAccount.Name;
            var storageAccountKey = await GetStorageAccountKey(storageAccount);
            var storageCreds = new Azure.Storage.Auth.StorageCredentials(storageAccountName, storageAccountKey);
            var csa = new CloudStorageAccount(storageCreds, useHttps: true);
            var fileClient = csa.CreateCloudFileClient();
            var fileShare = fileClient.GetShareReference(StorageMountableShareName);
            await fileShare.CreateIfNotExistsAsync();
        }

        /// <inheritdoc/>
        public async Task StartPrepareFileShareAsync(string azureStorageAccountId, string srcBlobUrl)
        {
            Requires.NotNullOrEmpty(azureStorageAccountId, nameof(azureStorageAccountId));
            Requires.NotNullOrEmpty(srcBlobUrl, nameof(srcBlobUrl));

            var azureSubscriptionId = GetAzureSubscriptionIdFromResourceId(azureStorageAccountId);

            var azure = await GetAzureClient(azureSubscriptionId);
            var storageAccount = await azure.StorageAccounts.GetByIdAsync(azureStorageAccountId);
            var storageAccountName = storageAccount.Name;
            var storageAccountKey = await GetStorageAccountKey(storageAccount);
            var storageCreds = new Azure.Storage.Auth.StorageCredentials(storageAccountName, storageAccountKey);
            var csa = new CloudStorageAccount(storageCreds, useHttps: true);

            var srcBlobUri = new Uri(srcBlobUrl);

            var fileClient = csa.CreateCloudFileClient();
            var fileShare = fileClient.GetShareReference(StorageMountableShareName);
            var destFile = fileShare.GetRootDirectoryReference().GetFileReference(StorageMountableFileName);

            await destFile.StartCopyAsync(srcBlobUri);
        }

        /// <inheritdoc/>
        public async Task<double> CheckPrepareFileShareAsync(string azureStorageAccountId)
        {
            Requires.NotNullOrEmpty(azureStorageAccountId, nameof(azureStorageAccountId));

            var azureSubscriptionId = GetAzureSubscriptionIdFromResourceId(azureStorageAccountId);

            var azure = await GetAzureClient(azureSubscriptionId);
            var storageAccount = await azure.StorageAccounts.GetByIdAsync(azureStorageAccountId);
            var storageAccountName = storageAccount.Name;
            var storageAccountKey = await GetStorageAccountKey(storageAccount);
            var storageCreds = new Azure.Storage.Auth.StorageCredentials(storageAccountName, storageAccountKey);
            var csa = new CloudStorageAccount(storageCreds, useHttps: true);
            var fileClient = csa.CreateCloudFileClient();
            var fileShare = fileClient.GetShareReference(StorageMountableShareName);
            var destFile = fileShare.GetRootDirectoryReference()
                .GetFileReference(StorageMountableFileName);

            await destFile.FetchAttributesAsync();

            var status = destFile.CopyState.Status;

            switch (status)
            {
                case CopyStatus.Success:
                    return 1;
                case CopyStatus.Pending:
                    return (double)destFile.CopyState.BytesCopied / (double)destFile.CopyState.TotalBytes;
                default:
                    throw new StoragePrepareException(azureStorageAccountId, destFile.Uri.ToString(), status.ToString());
            }
        }

        /// <inheritdoc/>
        public async Task<ShareConnectionInfo> GetConnectionInfoAsync(string azureStorageAccountId)
        {
            Requires.NotNullOrEmpty(azureStorageAccountId, nameof(azureStorageAccountId));

            var azureSubscriptionId = GetAzureSubscriptionIdFromResourceId(azureStorageAccountId);

            var azure = await GetAzureClient(azureSubscriptionId);
            var storageAccount = await azure.StorageAccounts.GetByIdAsync(azureStorageAccountId);
            var storageAccountName = storageAccount.Name;
            var storageAccountKey = await GetStorageAccountKey(storageAccount);
            var shareConnectionInfo = new ShareConnectionInfo(
                storageAccountName,
                storageAccountKey,
                StorageMountableShareName,
                StorageMountableFileName);
            return shareConnectionInfo;
        }

        /// <inheritdoc/>
        public async Task DeleteStorageAccountAsync(string azureStorageAccountId)
        {
            Requires.NotNullOrEmpty(azureStorageAccountId, nameof(azureStorageAccountId));

            var azureSubscriptionId = GetAzureSubscriptionIdFromResourceId(azureStorageAccountId);

            var azure = await GetAzureClient(azureSubscriptionId);
            await azure.StorageAccounts.DeleteByIdAsync(azureStorageAccountId);
        }

        private async Task<string> GenerateStorageAccountName(IAzure azure)
        {
            Requires.NotNull(azure, nameof(azure));

            var charsAvailable = StorageAccountNameMaxLength - StorageAccountNamePrefix.Length;

            for (var attempts = 0; attempts < StorageAccountNameGenerateMaxAttempts; attempts++)
            {
                var accountGuid = Guid.NewGuid().ToString("N").Substring(0, charsAvailable);
                var storageAccountName = string.Concat(StorageAccountNamePrefix, accountGuid);
                var checkNameAvailabilityResult = await azure.StorageAccounts
                    .CheckNameAvailabilityAsync(storageAccountName);
                if (checkNameAvailabilityResult.IsAvailable == true)
                {
                    return storageAccountName;
                }
            }

            throw new StorageCreateException("Unable to generate storage account name");
        }

        private async Task<IAzure> GetAzureClient(string azureSubscriptionId)
        {
            Requires.NotNullOrEmpty(azureSubscriptionId, nameof(azureSubscriptionId));

            try
            {
                var azureSub = this.systemCatalog
                    .AzureSubscriptionCatalog
                    .AzureSubscriptions
                    .Single(sub => sub.SubscriptionId == azureSubscriptionId && sub.Enabled);

                IServicePrincipal sp = azureSub.ServicePrincipal;
                string azureAppId = sp.ClientId;
                string azureAppKey = await sp.GetServicePrincipalClientSecret();
                string azureTenant = sp.TenantId;
                var creds = new AzureCredentialsFactory()
                    .FromServicePrincipal(
                        azureAppId,
                        azureAppKey,
                        azureTenant,
                        AzureEnvironment.AzureGlobalCloud);
                return Azure.Management.Fluent.Azure.Authenticate(creds)
                    .WithSubscription(azureSubscriptionId);
            }
            catch (InvalidOperationException)
            {
                throw new AzureClientException(azureSubscriptionId);
            }
        }

        private string GetAzureSubscriptionIdFromResourceId(string resourceId)
        {
            return ResourceId.FromString(resourceId).SubscriptionId;
        }

        private async Task<string> GetStorageAccountKey(Azure.Management.Storage.Fluent.IStorageAccount storageAccount)
        {
            var keys = await storageAccount.GetKeysAsync();
            var key1 = keys[0].Value;
            return key1;
        }
    }
}
