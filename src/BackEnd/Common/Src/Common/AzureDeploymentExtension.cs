// <copyright file="AzureDeploymentExtension.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.File;
using Microsoft.Azure.Storage.Queue;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common
{
    /// <summary>
    /// Azure deployment related utilities.
    /// </summary>
    public static class AzureDeploymentExtension
    {
        /// <summary>
        /// Create resource group if not exists.
        /// </summary>
        /// <param name="azure">azure client provider.</param>
        /// <param name="resourceGroupName">azure resource group name.</param>
        /// <param name="location">azure location where resource group will be created.</param>
        /// <returns>Task.</returns>
        public static async Task CreateResourceGroupIfNotExistsAsync(this IAzure azure, string resourceGroupName, string location)
        {
            if (await azure.ResourceGroups.ContainAsync(resourceGroupName))
            {
                return;
            }

            await azure.ResourceGroups.Define(resourceGroupName)
                  .WithRegion(location)
                  .CreateAsync();
        }

        /// <summary>
        /// Create storage account if not exists.
        /// </summary>
        /// <param name="azure"> azure client object.</param>
        /// <param name="resourceGroupName">azure resource group name.</param>
        /// <param name="location">azure region.</param>
        /// <param name="storageAccountName">azure storage account name.</param>
        /// <returns>Storage account object.</returns>
        public static async Task<IStorageAccount> CreateStorageAccountIfNotExistsAsync(
            this IAzure azure,
            string resourceGroupName,
            string location,
            string storageAccountName)
        {
            Requires.NotNullOrEmpty(resourceGroupName, nameof(resourceGroupName));
            Requires.NotNullOrEmpty(location, nameof(location));
            Requires.NotNullOrEmpty(storageAccountName, nameof(storageAccountName));

            var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(resourceGroupName, storageAccountName);
            if (storageAccount != null)
            {
                return storageAccount;
            }

            return await azure.StorageAccounts.Define(storageAccountName)
                 .WithRegion(location)
                 .WithExistingResourceGroup(resourceGroupName)
                 .WithGeneralPurposeAccountKindV2()
                 .WithOnlyHttpsTraffic()
                 .WithSku(StorageAccountSkuType.Standard_LRS)
                 .CreateAsync();
        }

        /// <summary>
        /// Delete Azure Resource group.
        /// </summary>
        /// <param name="azure">azure client provider.</param>
        /// <param name="resourceGroupName">azure resource group name.</param>
        /// <returns>Task.</returns>
        public static async Task DeleteResourceGroupAsync(this IAzure azure, string resourceGroupName)
        {
            Requires.NotNullOrEmpty(resourceGroupName, nameof(resourceGroupName));
            await azure.ResourceGroups
                .BeginDeleteByNameAsync(resourceGroupName);
        }

        /// <summary>
        /// Delete storage account.
        /// </summary>
        /// <param name="azure"> azure client object.</param>
        /// <param name="resourceGroupName">azure resource group name.</param>
        /// <param name="storageAccountName">azure storage account name.</param>
        /// <returns>Task.</returns>
        public static async Task DeleteStorageAccountAsync(
            this IAzure azure,
            string resourceGroupName,
            string storageAccountName)
        {
            Requires.NotNullOrEmpty(resourceGroupName, nameof(resourceGroupName));
            Requires.NotNullOrEmpty(storageAccountName, nameof(storageAccountName));

            await azure.StorageAccounts
                .DeleteByResourceGroupAsync(resourceGroupName, storageAccountName);
        }

        /// <summary>
        /// Get CloudQueue client.
        /// </summary>
        /// <param name="azure">azure client object.</param>
        /// <param name="resourceGroupName">azure resource group name.</param>
        /// <param name="storageAccountName">azure storage account name.</param>
        /// <param name="queueName">azure queue name.</param>
        /// <returns>cloud queue client.</returns>
        public static async Task<CloudQueue> GetCloudQueueClientAsync(this IAzure azure, string resourceGroupName, string storageAccountName, string queueName)
        {
            var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(resourceGroupName, storageAccountName);
            if (storageAccount == null)
            {
                // Queue is deleted as storage account does not exist
                throw new ArgumentException($"Could not find storage account {storageAccountName} in resource group {resourceGroupName}");
            }

            var storageClient = await GetStorageClient(storageAccount);

            // Create the queue client.
            var queueClient = storageClient.CreateCloudQueueClient();

            // Retrieve a reference to a container.
            return queueClient.GetQueueReference(queueName);
        }

        /// <summary>
        /// Get CloudFile client.
        /// </summary>
        /// <param name="azure">azure client object.</param>
        /// <param name="resourceGroupName">existing azure resource group name.</param>
        /// <param name="storageAccountName">existing azure storage account name.</param>
        /// <returns>cloud file client.</returns>
        public static async Task<CloudFileClient> GetCloudFileClientAsync(this IAzure azure, string resourceGroupName, string storageAccountName)
        {
            var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(resourceGroupName, storageAccountName);
            if (storageAccount == null)
            {
                // storage account does not exist
                throw new ArgumentException($"Could not find storage account {storageAccountName} in resource group {resourceGroupName}");
            }

            var csa = await GetStorageClient(storageAccount);

            // Create the file client.
            var client = csa.CreateCloudFileClient();

            // Retrieve a reference to a container.
            return client;
        }

        /// <summary>
        /// Deletes azure queue.
        /// </summary>
        /// <param name="azure">azure client object.</param>
        /// <param name="resourceGroupName">azure resource group name.</param>
        /// <param name="resourceName">azure queue name.</param>
        /// <param name="storageAccountName">azure storage account name.</param>
        /// <returns>Task.</returns>
        public static async Task DeleteQueueAsync(this IAzure azure, string resourceGroupName, string resourceName, string storageAccountName)
        {
            var queue = await azure.GetCloudQueueClientAsync(resourceGroupName, storageAccountName, resourceName);
            await queue.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Get Azure Storage client.
        /// </summary>
        /// <param name="storageAccount"> Storage account object.</param>
        /// <returns>Storage client.</returns>
        private static async Task<CloudStorageAccount> GetStorageClient(IStorageAccount storageAccount)
        {
            var storageAccountName = storageAccount.Name;
            var keys = await storageAccount.GetKeysAsync();
            var storageAccountKey = (keys.Count > 1) ? keys[0].Value : default;
            if (storageAccountKey == default)
            {
                throw new Exception($"Could not find storage account key for storage account : {storageAccountName}");
            }

            var storageCreds = new StorageCredentials(storageAccountName, storageAccountKey);
            var csa = new CloudStorageAccount(storageCreds, useHttps: true);
            return csa;
        }
    }
}