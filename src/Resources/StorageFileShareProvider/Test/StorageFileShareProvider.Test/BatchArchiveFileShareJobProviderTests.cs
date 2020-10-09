// <copyright file="BatchArchiveFileShareJobProviderTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Batch;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.File;
using Microsoft.Extensions.Configuration;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Settings;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Test
{
    public class BatchArchiveFileShareJobProviderTests
    {
        // Azure subscription to be used for tests
        private static readonly string azureSubscriptionId = "86642df6-843e-4610-a956-fdd497102261";
        // Prefix for Azure resource group that will be created (then cleaned-up) by the test
        private static readonly string azureResourceGroupPrefix = "test-storage-file-share-";
        // File share template info (storage account should be in same Azure subscription as above)
        private static readonly string fileShareTemplateStorageAccount = "vsodevciusw2siusw2";
        private static readonly string fileShareTemplateContainerName = "templates";

        private static readonly string fileShareTemplateBlobNameLinux = "cloudenvdata_kitchensink_1.0.1053-gcb237b7bbdd33bfa6c9fc4aee288e199e1a2b5d2.release160";
        // The name of the Windows blob is implied by the name of the Linux blob.
        // This is a limitation of the current schema for appsettings.images.json where only the image name is specified without knowledge of platform.
        // This works because both the Windows and Linux blobs are pushed at the same time with the same version, the Windows blob just has the ".disk.vhdx" postfix.
        private static readonly string fileShareTemplateBlobNameWindows = $"{fileShareTemplateBlobNameLinux}.disk.vhdx";
        private static readonly string batchAccountResourceGroup = "vsclk-online-dev-ci-usw2";
        private static readonly string batchAccountName = "vsodevciusw2bausw2";
        private static readonly string batchPoolId = "storage-worker-devstamp-pool";
        private static readonly string azureLocationStr = "westus2";
        private static readonly AzureLocation azureLocation = AzureLocation.WestUs2;
        private static readonly string azureSkuName = "Premium_LRS";
        private static readonly string azureStandardSkuName = "Standard_LRS";
        private static readonly string azureSubscriptionName = "ignorethis";
        private static readonly int ARCHIVE_TIMEOUT_MINS = 90;
        private static readonly int STORAGE_SIZE_IN_GB = 64;
        private static readonly int NUM_STORAGE_TO_CREATE = 1;

        /// <summary>
        /// Run all the operations exposed by the provider helper.
        /// These tests aren't mocked and so run against real Azure.
        /// It can take 10+ minutes for the file share preparation to complete.
        /// </summary>
        [Trait("Category", "IntegrationTest")]
        [Fact]
        public async Task BatchArchiveFileShareJobProvider_E2EFlow_Completes_Without_Errors()
        {
            var logger = new DefaultLoggerFactory().New();
            var taskHelper = new TaskHelper(logger);

            // Build common objects
            var resourceGroupName = GetResourceGroupName();
            var servicePrincipal = GetServicePrincipal();
            var catalogMoq = GetMockSystemCatalog(servicePrincipal);
            var azureClientFactory = GetAzureClientFactory(catalogMoq.Object);
            var azure = await GetAzureClient(azureClientFactory);
            var storageProviderSettings = new StorageProviderSettings() { WorkerBatchPoolId = batchPoolId };
            var resourceAccessorMoq = GetMockControlPlaneAzureResourceAccessor();
            var batchClientFactory = new BatchClientFactory(resourceAccessorMoq.Object);

            // Build real objects
            var providerHelper = new StorageFileShareProviderHelper(azureClientFactory);
            var batchPrepareFileShareJobProvider = new BatchPrepareFileShareJobProvider(
                providerHelper, batchClientFactory, azureClientFactory, storageProviderSettings);
            var batchArchiveFileShareJobProvider = new BatchArchiveFileShareJobProvider(
                providerHelper, batchClientFactory, azureClientFactory, storageProviderSettings);

            // Create storage accounts
            var storageAccount = await providerHelper.CreateStorageAccountAsync(
                azureSubscriptionId, azureLocationStr, resourceGroupName, azureSkuName, new Dictionary<string, string> { { "ResourceTag", "GeneratedFromTest" }, }, logger);
            var destStorageAccount = await providerHelper.CreateStorageAccountAsync(
                azureSubscriptionId, azureLocationStr, resourceGroupName, azureStandardSkuName, new Dictionary<string, string> { { "ResourceTag", "GeneratedFromTest" }, }, logger);

            try
            {
                // Run through each test
                await taskHelper.RunConcurrentEnumerableAsync(
                    "run_workflow",
                    Enumerable.Range(0, NUM_STORAGE_TO_CREATE),
                    async (i, childLogger) =>
                    {
                        // Create file share
                        await providerHelper.CreateFileShareAsync(storageAccount, STORAGE_SIZE_IN_GB, childLogger);

                        // Start prepare file share job
                        var linuxCopyItem = new StorageCopyItem()
                        {
                            SrcBlobUrl = await GetSrcBlobUrlAsync(azure, fileShareTemplateBlobNameLinux),
                            StorageType = StorageType.Linux,
                        };
                        var windowsCopyItem = new StorageCopyItem()
                        {
                            SrcBlobUrl = await GetSrcBlobUrlAsync(azure, fileShareTemplateBlobNameWindows),
                            StorageType = StorageType.Windows,
                        };
                        var prepareBatchInfo = await batchPrepareFileShareJobProvider.StartPrepareFileShareAsync(
                            storageAccount, new[] { linuxCopyItem, windowsCopyItem }, STORAGE_SIZE_IN_GB, childLogger);

                        // Check prepare file share status
                        var startPrepareTime = DateTime.Now;
                        await taskHelper.RetryUntilSuccessOrTimeout(
                            "run_workflow_check_prepare_status",
                            async (innerLogger) =>
                            {
                                var taskMaxWaitTime = default(TimeSpan);
                                var result = await batchPrepareFileShareJobProvider.CheckBatchTaskStatusAsync(storageAccount, prepareBatchInfo, taskMaxWaitTime, innerLogger);
                                if (result == BatchTaskStatus.Failed)
                                {
                                    throw new Exception("Prepare batch operation failed.");
                                }

                                return result == BatchTaskStatus.Succeeded;
                            },
                            TimeSpan.FromMinutes(ARCHIVE_TIMEOUT_MINS),
                            TimeSpan.FromSeconds(10),
                            childLogger,
                            () => throw new Exception($"Failed to complete all file share preparations in given time of {ARCHIVE_TIMEOUT_MINS} minutes."));
                        var runPrepareTime = (DateTime.Now - startPrepareTime).TotalSeconds;

                        // Start batch upload job
                        var srcFileShare = await SrcFileShareSasToken(storageAccount, providerHelper, logger);
                        var destBlob = await GetDestBlobSasToken(destStorageAccount, destStorageAccount.Name, providerHelper, logger);
                        var archiveBatchInfo = await batchArchiveFileShareJobProvider.StartArchiveFileShareAsync(
                            storageAccount, srcFileShare.Token, destBlob.Token, childLogger);

                        // Check prepare file share status
                        var startArchiveTime = DateTime.Now;
                        await taskHelper.RetryUntilSuccessOrTimeout(
                            "run_workflow_check_archive_status",
                            async (innerLogger) =>
                            {
                                var taskMaxWaitTime = default(TimeSpan);
                                var result = await batchArchiveFileShareJobProvider.CheckBatchTaskStatusAsync(storageAccount, archiveBatchInfo, taskMaxWaitTime, innerLogger);
                                if (result == BatchTaskStatus.Failed)
                                {
                                    throw new Exception("Archive batch operation failed.");
                                }

                                return result == BatchTaskStatus.Succeeded;
                            },
                            TimeSpan.FromMinutes(ARCHIVE_TIMEOUT_MINS),
                            TimeSpan.FromSeconds(10),
                            childLogger,
                            () => throw new Exception($"Failed to complete all file share preparations in given time of {ARCHIVE_TIMEOUT_MINS} minutes."));
                        var runArchiveTime = (DateTime.Now - startArchiveTime).TotalSeconds;

                    },
                    logger);
            }
            finally
            {
                // Force cleanup
                await providerHelper.DeleteStorageAccountAsync(storageAccount, logger);
                await providerHelper.DeleteStorageAccountAsync(destStorageAccount, logger);
            }
        }

        private static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
            return config;
        }

        private static string GetResourceGroupName()
        {
            return azureResourceGroupPrefix + Environment.UserName;
        }

        private static IServicePrincipal GetServicePrincipal()
        {
            var config = InitConfiguration();

            var clientId = config["CLIENT_ID"];
            var tenantId = config["TENANT_ID"];
            var clientSecret = config["CLIENT_SECRET"];
            var secretProviderMoq = new Mock<ISecretProvider>();
            secretProviderMoq
                .Setup(p => p.GetSecretAsync(It.IsAny<string>()))
                .ReturnsAsync(clientSecret);

            return new ServicePrincipal(clientId, "ignorethis", tenantId, secretProviderMoq.Object);
        }

        private static Mock<ISystemCatalog> GetMockSystemCatalog(IServicePrincipal servicePrincipal)
        {
            var catalogMoq = new Mock<ISystemCatalog>();
            catalogMoq
                .Setup(x => x.AzureSubscriptionCatalog.AzureSubscriptions)
                .Returns(new[] {
                    new AzureSubscription(
                        azureSubscriptionId,
                        azureSubscriptionName,
                        servicePrincipal,
                        true,
                        new[] {azureLocation},
                        null,
                        null,
                        null,
                        100,
                        null)
                });
            return catalogMoq;
        }

        private static Mock<IControlPlaneAzureResourceAccessor> GetMockControlPlaneAzureResourceAccessor()
        {
            var resourceAccessor = new Mock<IControlPlaneAzureResourceAccessor>();
            resourceAccessor.Setup(x => x.GetStampBatchAccountAsync(It.IsAny<AzureLocation>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(async () =>
                {
                    // Get Credentials 
                    var config = InitConfiguration();
                    var clientId = config["CLIENT_ID"];
                    var tenantId = config["TENANT_ID"];
                    var clientSecret = config["CLIENT_SECRET"];

                    // Create client to access batch
                    var creds = new AzureCredentialsFactory()
                    .FromServicePrincipal(
                        clientId,
                        clientSecret,
                        tenantId,
                        AzureEnvironment.AzureGlobalCloud);
                    var batchManagementClient = new BatchManagementClient(creds)
                    {
                        SubscriptionId = azureSubscriptionId,
                    };

                    // Get return values from batch client
                    var batchAccount = await batchManagementClient.BatchAccount.GetAsync(
                        batchAccountResourceGroup,
                        batchAccountName);
                    var accountKeys = await batchManagementClient.BatchAccount.GetKeysAsync(
                        batchAccountResourceGroup,
                        batchAccountName);
                    var batchKey = accountKeys.Primary;

                    return (batchAccount.Name, batchKey, $"https://{batchAccount.AccountEndpoint}");
                });

            return resourceAccessor;
        }

        private static IAzureClientFactory GetAzureClientFactory(ISystemCatalog catalog)
        {
            return new AzureClientFactory(catalog.AzureSubscriptionCatalog);
        }

        private static async Task<IAzure> GetAzureClient(IAzureClientFactory azureClientFactory)
        {
            var azure = await azureClientFactory.GetAzureClientAsync(new Guid(azureSubscriptionId));
            return azure;
        }

        private static async Task<string> GetSrcBlobUrlAsync(IAzure azure, string srcBlobName)
        {
            // Get storage account key
            var storageAccountsInSubscription = await azure.StorageAccounts.ListAsync();
            var srcStorageAccount = storageAccountsInSubscription.Single(sa => sa.Name == fileShareTemplateStorageAccount);
            var srcStorageAccountKey = (await srcStorageAccount.GetKeysAsync())[0].Value;

            // Create blob sas url
            var storageCreds = new StorageCredentials(srcStorageAccount.Name, srcStorageAccountKey);
            var blobRef = new CloudStorageAccount(storageCreds, useHttps: true)
                .CreateCloudBlobClient()
                .GetContainerReference(fileShareTemplateContainerName)
                .GetBlobReference(srcBlobName);
            var blobSas = blobRef.GetSharedAccessSignature(new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(12) // Allow time for blob copy and debugging, etc.
            });
            return blobRef.Uri + blobSas;
        }

        private static Task<(string Token, string BlobName, string BlobContainerName)> GetDestBlobSasToken(
            AzureResourceInfo info, string blobName, IStorageFileShareProviderHelper storageFileShareProviderHelper, IDiagnosticsLogger logger)
        {
            return storageFileShareProviderHelper.FetchArchiveBlobSasTokenAsync(
                info, blobName, null, SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Read, logger);
        }

        private static Task<(string Token, string FileShareName, string FileName)> SrcFileShareSasToken(
            AzureResourceInfo info, IStorageFileShareProviderHelper storageFileShareProviderHelper, IDiagnosticsLogger logger)
        {
            return storageFileShareProviderHelper.FetchStorageFileShareSasTokenAsync(
                info, null, StorageType.Linux, SharedAccessFilePermissions.Read, null, logger);
        }
    }
}
