// <copyright file="StorageFileShareProviderHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
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
        private static readonly int StorageAccountNameMaxLength = 24;
        private static readonly int StorageAccountNameGenerateMaxAttempts = 3;
        private static readonly string StorageMountableShareName = "cloudenvdata";
        private static readonly string StorageMountableFileName = "dockerlib";
        private static readonly string StorageAccountNamePrefix = "vsoce";
        private readonly ISystemCatalog systemCatalog;
        private readonly IAzureClientFactory azureClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageFileShareProviderHelper"/> class.
        /// </summary>
        /// <param name="systemCatalog">System catalog.</param>
        public StorageFileShareProviderHelper(ISystemCatalog systemCatalog)
        {
            this.systemCatalog = Requires.NotNull(systemCatalog, nameof(systemCatalog));
            azureClientFactory = new AzureClientFactory(this.systemCatalog);
        }

        /// <inheritdoc/>
        public async Task<AzureResourceInfo> CreateStorageAccountAsync(
            string azureSubscriptionId,
            string azureRegion,
            string azureResourceGroup,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(azureRegion, nameof(azureRegion));
            Requires.NotNullOrEmpty(azureResourceGroup, nameof(azureResourceGroup));
            Requires.NotNullOrEmpty(azureSubscriptionId, nameof(azureSubscriptionId));

            logger = logger.WithValues(new LogValueSet
            {
                { "AzureRegion", azureRegion },
                { "AzureResourceGroup", azureResourceGroup },
                { "AzureSubscription", azureSubscriptionId },
            });
            var azure = await azureClientFactory.GetAzureClientAsync(new Guid(azureSubscriptionId));

            try
            {
                await azure.CreateResourceGroupIfNotExistsAsync(azureResourceGroup, azureRegion);
                var storageAccountName = await GenerateStorageAccountName(azure, logger);
                var storageAccount = await azure.StorageAccounts.Define(storageAccountName)
                    .WithRegion(azureRegion)
                    .WithExistingResourceGroup(azureResourceGroup)
                    .WithGeneralPurposeAccountKindV2()
                    .WithOnlyHttpsTraffic()
                    .WithSku(StorageAccountSkuType.Standard_LRS)
                    .CreateAsync();

                logger.FluentAddValue("AzureStorageAccountName", storageAccountName)
                    .LogInfo("file_share_storage_provider_helper_create_storage_account_complete");

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
                var csa = new CloudStorageAccount(storageCreds, useHttps: true);
                var fileClient = csa.CreateCloudFileClient();
                var fileShare = fileClient.GetShareReference(StorageMountableShareName);
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
        public async Task StartPrepareFileShareAsync(
            AzureResourceInfo azureResourceInfo,
            string srcBlobUrl,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            Requires.NotNullOrEmpty(srcBlobUrl, nameof(srcBlobUrl));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            var azureSubscriptionId = azureResourceInfo.SubscriptionId;
            var azure = await azureClientFactory.GetAzureClientAsync(azureSubscriptionId);

            try
            {
                var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(azureResourceInfo.ResourceGroup, azureResourceInfo.Name);
                var storageAccountName = storageAccount.Name;
                var storageAccountKey = await GetStorageAccountKey(storageAccount);
                var storageCreds = new StorageCredentials(storageAccountName, storageAccountKey);
                var csa = new CloudStorageAccount(storageCreds, useHttps: true);

                var srcBlobUri = new Uri(srcBlobUrl);

                var fileClient = csa.CreateCloudFileClient();
                var fileShare = fileClient.GetShareReference(StorageMountableShareName);
                var destFile = fileShare.GetRootDirectoryReference().GetFileReference(StorageMountableFileName);

                await destFile.StartCopyAsync(srcBlobUri);

                logger.FluentAddValue("DestinationStorageFilePath", destFile.Uri.ToString())
                    .LogInfo("file_share_storage_provider_helper_start_prepare_file_share_complete");
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_start_prepare_file_share_error", ex);

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<double> CheckPrepareFileShareAsync(
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
                var csa = new CloudStorageAccount(storageCreds, useHttps: true);
                var fileClient = csa.CreateCloudFileClient();
                var fileShare = fileClient.GetShareReference(StorageMountableShareName);
                var destFile = fileShare.GetRootDirectoryReference()
                    .GetFileReference(StorageMountableFileName);

                await destFile.FetchAttributesAsync();

                var status = destFile.CopyState.Status;

                double completedAmount;

                switch (status)
                {
                    case CopyStatus.Success:
                        completedAmount = 1;
                        break;
                    case CopyStatus.Pending:
                        completedAmount = (double)destFile.CopyState.BytesCopied / (double)destFile.CopyState.TotalBytes;
                        break;
                    default:
                        var copyStatus = status.ToString();
                        var copyStatusDesc = destFile.CopyState.StatusDescription;
                        var destFilePath = destFile.Uri.ToString();

                        logger.FluentAddValue("CopyStatus", copyStatus)
                            .FluentAddValue("CopyStatusDescription", copyStatusDesc)
                            .FluentAddValue("DestinationStorageFilePath", destFilePath)
                            .LogError("file_share_storage_provider_helper_check_prepare_file_share_error");

                        throw new StoragePrepareException(azureResourceInfo, destFilePath, copyStatus, copyStatusDesc);
                }

                logger.FluentAddValue("CompletedAmount", completedAmount.ToString())
                    .LogInfo("file_share_storage_provider_helper_check_prepare_file_share_complete");

                return completedAmount;
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_check_prepare_file_share_error", ex);

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ShareConnectionInfo> GetConnectionInfoAsync(
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
                var shareConnectionInfo = new ShareConnectionInfo(
                    storageAccountName,
                    storageAccountKey,
                    StorageMountableShareName,
                    StorageMountableFileName);
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
                await azure.ResourceGroups.BeginDeleteByNameAsync(azureResourceInfo.ResourceGroup);
                logger.LogInfo("file_share_storage_provider_helper_delete_storage_account_complete");
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_delete_storage_account_error", ex);
                throw;
            }
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

        private async Task<string> GetStorageAccountKey(IStorageAccount storageAccount)
        {
            var keys = await storageAccount.GetKeysAsync();
            var key1 = keys[0].Value;
            return key1;
        }
    }
}
