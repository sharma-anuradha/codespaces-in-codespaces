// <copyright file="StorageFileShareProviderHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
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
        public async Task<string> CreateStorageAccountAsync(
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
                await azure.CreateIfNotExistsResourceGroupAsync(azureResourceGroup, azureRegion);
                var storageAccountName = await GenerateStorageAccountName(azure, logger);
                var storageAccount = await azure.StorageAccounts.Define(storageAccountName)
                    .WithRegion(azureRegion)
                    .WithExistingResourceGroup(azureResourceGroup)
                    .WithGeneralPurposeAccountKindV2()
                    .WithOnlyHttpsTraffic()
                    .WithSku(StorageAccountSkuType.Standard_LRS)
                    .CreateAsync();

                var storageAccountId = storageAccount.Id;
                logger
                    .FluentAddValue("StorageAccountName", storageAccountName)
                    .FluentAddValue("AzureStorageAccountId", storageAccountId)
                    .LogInfo("file_share_storage_provider_helper_create_storage_account_complete");
                return storageAccountId;
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_create_storage_account_error", ex);
                throw ex;
            }
        }

        /// <inheritdoc/>
        public async Task CreateFileShareAsync(
            string azureStorageAccountId,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(azureStorageAccountId, nameof(azureStorageAccountId));
            logger = logger.WithValue("AzureStorageAccountId", azureStorageAccountId);

            var azureSubscriptionId = GetAzureSubscriptionIdFromResourceId(azureStorageAccountId);
            var azure = await azureClientFactory.GetAzureClientAsync(new Guid(azureSubscriptionId));

            try
            {
                var storageAccount = await azure.StorageAccounts.GetByIdAsync(azureStorageAccountId);
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
            string azureStorageAccountId,
            string srcBlobUrl,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(azureStorageAccountId, nameof(azureStorageAccountId));
            Requires.NotNullOrEmpty(srcBlobUrl, nameof(srcBlobUrl));
            logger = logger.WithValue("AzureStorageAccountId", azureStorageAccountId);

            var azureSubscriptionId = GetAzureSubscriptionIdFromResourceId(azureStorageAccountId);
            var azure = await azureClientFactory.GetAzureClientAsync(new Guid(azureSubscriptionId));

            try
            {
                var storageAccount = await azure.StorageAccounts.GetByIdAsync(azureStorageAccountId);
                var storageAccountName = storageAccount.Name;
                var storageAccountKey = await GetStorageAccountKey(storageAccount);
                var storageCreds = new StorageCredentials(storageAccountName, storageAccountKey);
                var csa = new CloudStorageAccount(storageCreds, useHttps: true);

                var srcBlobUri = new Uri(srcBlobUrl);

                var fileClient = csa.CreateCloudFileClient();
                var fileShare = fileClient.GetShareReference(StorageMountableShareName);
                var destFile = fileShare.GetRootDirectoryReference().GetFileReference(StorageMountableFileName);

                await destFile.StartCopyAsync(srcBlobUri);

                logger
                    .FluentAddValue("DestinationStorageFilePath", destFile.Uri.ToString())
                    .LogInfo("file_share_storage_provider_helper_start_prepare_file_share_complete");
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_start_prepare_file_share_error", ex);
                throw ex;
            }
        }

        /// <inheritdoc/>
        public async Task<double> CheckPrepareFileShareAsync(
            string azureStorageAccountId,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(azureStorageAccountId, nameof(azureStorageAccountId));
            logger = logger.WithValue("AzureStorageAccountId", azureStorageAccountId);

            var azureSubscriptionId = GetAzureSubscriptionIdFromResourceId(azureStorageAccountId);
            var azure = await azureClientFactory.GetAzureClientAsync(new Guid(azureSubscriptionId));

            try
            {
                var storageAccount = await azure.StorageAccounts.GetByIdAsync(azureStorageAccountId);
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
                        logger
                            .FluentAddValue("CopyStatus", copyStatus)
                            .FluentAddValue("CopyStatusDescription", copyStatusDesc)
                            .FluentAddValue("DestinationStorageFilePath", destFilePath)
                            .LogError("file_share_storage_provider_helper_check_prepare_file_share_error");
                        throw new StoragePrepareException(azureStorageAccountId, destFilePath, copyStatus, copyStatusDesc);
                }

                logger
                    .FluentAddValue("CompletedAmount", completedAmount.ToString())
                    .LogInfo("file_share_storage_provider_helper_check_prepare_file_share_complete");
                return completedAmount;
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_check_prepare_file_share_error", ex);
                throw ex;
            }
        }

        /// <inheritdoc/>
        public async Task<ShareConnectionInfo> GetConnectionInfoAsync(
            string azureStorageAccountId,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(azureStorageAccountId, nameof(azureStorageAccountId));
            logger = logger.WithValue("AzureStorageAccountId", azureStorageAccountId);

            var azureSubscriptionId = GetAzureSubscriptionIdFromResourceId(azureStorageAccountId);
            var azure = await azureClientFactory.GetAzureClientAsync(new Guid(azureSubscriptionId));

            try
            {
                var storageAccount = await azure.StorageAccounts.GetByIdAsync(azureStorageAccountId);
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
            string azureStorageAccountId,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(azureStorageAccountId, nameof(azureStorageAccountId));
            logger = logger.WithValue("AzureStorageAccountId", azureStorageAccountId);

            var azureSubscriptionId = GetAzureSubscriptionIdFromResourceId(azureStorageAccountId);
            var azure = await azureClientFactory.GetAzureClientAsync(new Guid(azureSubscriptionId));

            try
            {
                await azure.StorageAccounts.DeleteByIdAsync(azureStorageAccountId);
                logger.LogInfo("file_share_storage_provider_helper_delete_storage_account_complete");
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_delete_storage_account_error", ex);
                throw ex;
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

        private string GetAzureSubscriptionIdFromResourceId(string resourceId)
        {
            return ResourceId.FromString(resourceId).SubscriptionId;
        }

        private async Task<string> GetStorageAccountKey(IStorageAccount storageAccount)
        {
            var keys = await storageAccount.GetKeysAsync();
            var key1 = keys[0].Value;
            return key1;
        }
    }
}
