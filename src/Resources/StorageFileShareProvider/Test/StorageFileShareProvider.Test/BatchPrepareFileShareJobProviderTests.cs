// <copyright file="BatchPrepareFileShareJobProviderTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Batch;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
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
    public class BatchPrepareFileShareJobProviderTests
    {
        // Azure subscription to be used for tests
        private static readonly string srcAzureSubscriptionId = "86642df6-843e-4610-a956-fdd497102261";

        // File share template info (storage account should be in same Azure subscription as above)
        private static readonly string srcTemplateStorageAccount = "vsodevciusw2siusw2";

        // Blob container name that holds the file share templates.
        private static readonly string srcTemplateStorageContainerName = "templates";

        // Blob container name that holds the file share templates.
        private static readonly string srcTemplateBlobNameLinux = "cloudenvdata_kitchensink_1.0.2583-g656088a7fb87cca16ccdc6cc8686f34342a71260.release1104";
        
        // Azure subscription that the storage accounts will be created in.
        private static readonly string destAzureSubscriptionId = "6cb4f993-a8bb-422a-9864-1346e1b4dd2c";

        // Prefix for Azure resource group that will be created (then cleaned-up) by the test
        private static readonly string destResourceGroupPrefix = "test-storage-file-share-";

        private static readonly string destFileShareStorageSkuName = "Premium_LRS";

        // Resource group of the batch account in the control-plane subscription
        private static readonly string batchAccountResourceGroup = "vsclk-online-dev-ci-usw2";

        // Account name of the batch account in the control-plane subscription
        private static readonly string batchAccountName = "vsodevciusw2bausw2";

        // Pool id of the batch account in the control-plane subscription
        private static readonly string batchPoolId = "storage-worker-devstamp-pool";
        private static readonly string azureLocationStr = "westus2";
        private static readonly AzureLocation azureLocation = AzureLocation.WestUs2;
        private static readonly string azureSubscriptionName = "ignorethis";
        private static readonly int PREPARE_TIMEOUT_MINS = 30;
        private static readonly int STORAGE_SIZE_IN_GB = 64;
        private static readonly int NUM_STORAGE_TO_CREATE = 1;

        private static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
            return config;
        }

        private static string GetFileShareStorageResourceGroupName()
        {
            return destResourceGroupPrefix + Environment.UserName;
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
            var infraSubscription = new AzureSubscription(
                srcAzureSubscriptionId,
                azureSubscriptionName,
                servicePrincipal,
                true,
                new[] { azureLocation },
                null,
                null,
                null,
                100,
                null);
            var dataPlaneSubscription = new AzureSubscription(
                destAzureSubscriptionId,
                azureSubscriptionName,
                servicePrincipal,
                true,
                new[] { azureLocation },
                null,
                null,
                null,
                100,
                null);
            catalogMoq
                .Setup(x => x.AzureSubscriptionCatalog.AzureSubscriptions)
                .Returns(new[] {
                    dataPlaneSubscription
                });
            catalogMoq
                .Setup(x => x.AzureSubscriptionCatalog.InfrastructureSubscription)
                .Returns(infraSubscription);
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
                        SubscriptionId = srcAzureSubscriptionId,
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

        private static async Task<IAzure> GetAzureClient(IAzureClientFactory azureClientFactory, string subscriptionId)
        {
            var azure = await azureClientFactory.GetAzureClientAsync(new Guid(subscriptionId));
            return azure;
        }

        private static async Task<string> GetSrcBlobUrlAsync(IAzure azure, string srcBlobName)
        {
            // Get storage account key
            var storageAccountsInSubscription = await azure.StorageAccounts.ListAsync();
            var srcStorageAccount = storageAccountsInSubscription.Single(sa => sa.Name == srcTemplateStorageAccount);
            var srcStorageAccountKey = (await srcStorageAccount.GetKeysAsync())[0].Value;

            // Create blob sas url
            var storageCreds = new StorageCredentials(srcStorageAccount.Name, srcStorageAccountKey);
            var blobRef = new CloudStorageAccount(storageCreds, useHttps: true)
                .CreateCloudBlobClient()
                .GetContainerReference(srcTemplateStorageContainerName)
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

            var servicePrincipal = GetServicePrincipal();
            var catalogMoq = GetMockSystemCatalog(servicePrincipal);
            var azureClientFactory = new AzureClientFactory(catalogMoq.Object.AzureSubscriptionCatalog);
            var storageProviderSettings = new StorageProviderSettings() { WorkerBatchPoolId = batchPoolId };
            var srcAzureClient = await GetAzureClient(azureClientFactory, srcAzureSubscriptionId);
            var resourceAccessorMoq = GetMockControlPlaneAzureResourceAccessor();
            var batchClientFactory = new BatchClientFactory(resourceAccessorMoq.Object);

            // construct the real StorageFileShareProviderHelper
            IStorageFileShareProviderHelper providerHelper = new StorageFileShareProviderHelper(
                azureClientFactory);

            IBatchPrepareFileShareJobProvider batchPrepareFileShareJobProvider = new BatchPrepareFileShareJobProvider(
                providerHelper, batchClientFactory, azureClientFactory, storageProviderSettings);

            // Create storage accounts
            var fileShareStorageResourceGroupName = GetFileShareStorageResourceGroupName();
            var storageAccounts = await Task.WhenAll(
                Enumerable.Range(0, NUM_STORAGE_TO_CREATE)
                    .Select(x => providerHelper.CreateStorageAccountAsync(
                        destAzureSubscriptionId,
                        azureLocationStr,
                        fileShareStorageResourceGroupName,
                        destFileShareStorageSkuName,
                        new Dictionary<string, string> { { "ResourceTag", "GeneratedFromTest" }, },
                        logger))
            );

            try
            {
                // Create file shares
                await Task.WhenAll(storageAccounts.Select(sa => providerHelper.CreateFileShareAsync(sa, STORAGE_SIZE_IN_GB, logger)));

                var linuxCopyItem = new StorageCopyItem()
                {
                    SrcBlobUrl = await GetSrcBlobUrlAsync(srcAzureClient, srcTemplateBlobNameLinux),
                    StorageType = StorageType.Linux,
                };

                var prepareFileShareTaskInfos = await Task.WhenAll(storageAccounts.Select(sa => batchPrepareFileShareJobProvider.StartPrepareFileShareAsync(sa, new[] { linuxCopyItem }, STORAGE_SIZE_IN_GB, logger)));

                var fileShareStatus = new BatchTaskStatus[NUM_STORAGE_TO_CREATE];
                var taskMaxWaitTime = TimeSpan.FromMinutes(30);

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                while (fileShareStatus.Any(x => x == BatchTaskStatus.Pending || x == BatchTaskStatus.Running) && stopWatch.Elapsed < TimeSpan.FromMinutes(PREPARE_TIMEOUT_MINS))
                {
                    Thread.Sleep(TimeSpan.FromSeconds(60));
                    fileShareStatus = await Task.WhenAll(storageAccounts.Zip(prepareFileShareTaskInfos, (sa, prepareInfo) => batchPrepareFileShareJobProvider.CheckBatchTaskStatusAsync(sa, prepareInfo, taskMaxWaitTime, logger)));
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
