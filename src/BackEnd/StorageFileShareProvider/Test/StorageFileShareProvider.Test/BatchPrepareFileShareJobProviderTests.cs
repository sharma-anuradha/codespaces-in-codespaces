// <copyright file="BatchPrepareFileShareJobProviderTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Settings;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Test
{
    public class BatchPrepareFileShareJobProviderTests
    {
        // Azure subscription to be used for tests
        private static readonly string azureSubscriptionId = "86642df6-843e-4610-a956-fdd497102261";
        // Prefix for Azure resource group that will be created (then cleaned-up) by the test
        private static readonly string azureResourceGroupPrefix = "test-storage-file-share-";
        // File share template info (storage account should be in same Azure subscription as above)
        private static readonly string fileShareTemplateStorageAccount = "vsodevciusw2siusw2";
        private static readonly string fileShareTemplateContainerName = "templates";

        private static readonly string fileShareTemplateBlobNameLinux = "cloudenvdata_kitchensink_1.0.2089-g1dc5bc293ef42efa683585df9031b92ba7cd1a75.release765";
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
        private static readonly string azureSubscriptionName = "ignorethis";
        private static readonly int PREPARE_TIMEOUT_MINS = 30;
        private static readonly int STORAGE_SIZE_IN_GB = 64;
        private static readonly int NUM_STORAGE_TO_CREATE = 2;

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
            var testSubscription = new AzureSubscription(
                azureSubscriptionId,
                azureSubscriptionName,
                servicePrincipal,
                true,
                new[] { azureLocation },
                null,
                null,
                null);
            catalogMoq
                .Setup(x => x.AzureSubscriptionCatalog.AzureSubscriptions)
                .Returns(new[] {
                    testSubscription
                });
            catalogMoq
                .Setup(x => x.AzureSubscriptionCatalog.InfrastructureSubscription)
                .Returns(testSubscription);
            return catalogMoq;
        }

        private static Mock<IControlPlaneAzureResourceAccessor> GetMockControlPlaneAzureResourceAccessor(IAzure azure)
        {
            var resourceAccessor = new Mock<IControlPlaneAzureResourceAccessor>();
            resourceAccessor.Setup(x => x.GetStampBatchAccountAsync(It.IsAny<AzureLocation>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(async () =>
                {
                    var batch = await azure.BatchAccounts.GetByResourceGroupAsync(batchAccountResourceGroup, batchAccountName);
                    var batchKey = batch.GetKeys().Primary;
                    return (batch.Name, batchKey, $"https://{batch.AccountEndpoint}");
                });
            return resourceAccessor;
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

        /// <summary>
        /// Run all the operations exposed by the provider helper.
        /// These tests aren't mocked and so run against real Azure.
        /// It can take 10+ minutes for the file share preparation to complete.
        /// </summary>
        [Trait("Category", "IntegrationTest")]
        [Fact]
        public async Task BatchPrepareFileShareJobProvider_E2EFlow_Completes_Without_Errors()
        {
            var logger = new DefaultLoggerFactory().New();

            var resourceGroupName = GetResourceGroupName();
            var servicePrincipal = GetServicePrincipal();
            var catalogMoq = GetMockSystemCatalog(servicePrincipal);
            var azureClientFactory = new AzureClientFactory(catalogMoq.Object.AzureSubscriptionCatalog);
            var azure = await GetAzureClient(azureClientFactory);
            var storageProviderSettings = new StorageProviderSettings() { WorkerBatchPoolId = batchPoolId };
            var resourceAccessorMoq = GetMockControlPlaneAzureResourceAccessor(azure);
            var batchClientFactory = new BatchClientFactory(resourceAccessorMoq.Object);

            // construct the real StorageFileShareProviderHelper
            IStorageFileShareProviderHelper providerHelper = new StorageFileShareProviderHelper(
                azureClientFactory);

            IBatchPrepareFileShareJobProvider batchPrepareFileShareJobProvider = new BatchPrepareFileShareJobProvider(
                providerHelper, batchClientFactory, azureClientFactory, storageProviderSettings);

            // Create storage accounts
            var storageAccounts = await Task.WhenAll(
                Enumerable.Range(0, NUM_STORAGE_TO_CREATE)
                    .Select(x => providerHelper.CreateStorageAccountAsync(
                        azureSubscriptionId,
                        azureLocationStr,
                        resourceGroupName,
                        azureSkuName,
                        new Dictionary<string, string> { { "ResourceTag", "GeneratedFromTest" }, },
                        logger))
            );

            try
            {
                // Create file shares
                await Task.WhenAll(storageAccounts.Select(sa => providerHelper.CreateFileShareAsync(sa, logger)));

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

                var prepareFileShareTaskInfos = await Task.WhenAll(storageAccounts.Select(sa => batchPrepareFileShareJobProvider.StartPrepareFileShareAsync(sa, new[] { linuxCopyItem, windowsCopyItem }, STORAGE_SIZE_IN_GB, logger)));

                var fileShareStatus = new BatchTaskStatus[NUM_STORAGE_TO_CREATE];

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                while (fileShareStatus.Any(x => x == BatchTaskStatus.Pending || x == BatchTaskStatus.Running) && stopWatch.Elapsed < TimeSpan.FromMinutes(PREPARE_TIMEOUT_MINS))
                {
                    Thread.Sleep(TimeSpan.FromSeconds(60));
                    fileShareStatus = await Task.WhenAll(storageAccounts.Zip(prepareFileShareTaskInfos, (sa, prepareInfo) => batchPrepareFileShareJobProvider.CheckBatchTaskStatusAsync(sa, prepareInfo, logger)));
                }

                stopWatch.Stop();

                // Verify that none still haven't finished after the timeout
                if (fileShareStatus.Any(x => x != BatchTaskStatus.Succeeded))
                {
                    Assert.True(false, string.Format("Failed to complete all file share preparations in given time of {0} minutes.", PREPARE_TIMEOUT_MINS));
                }
            }
            finally
            {
                // Verify that we can delete the storage accounts
                await Task.WhenAll(storageAccounts.Select(sa => providerHelper.DeleteStorageAccountAsync(sa, logger)));
            }
        }
    }
}
