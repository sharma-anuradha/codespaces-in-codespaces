// <copyright file="StorageFileShareProviderHelperTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Test
{
    public class StorageFileShareProviderHelperTests
    {
        // Azure subscription to be used for tests
        private static readonly string azureSubscriptionId = "86642df6-843e-4610-a956-fdd497102261";
        // Prefix for Azure resource group that will be created (then cleaned-up) by the test
        private static readonly string azureResourceGroupPrefix = "test-storage-file-share-";
        // File share template info (storage account should be in same Azure subscription as above)
        private static readonly string fileShareTemplateStorageAccount = "vsclkcloudenvstbaseusw2";
        private static readonly string fileShareTemplateContainerName = "ext4-images";
        private static readonly string fileShareTemplateBlobName = "cloudenvdata_2964550";

        private static readonly string azureLocationStr = "westus2";
        private static readonly AzureLocation azureLocation = AzureLocation.WestUs2;
        private static readonly string azureSubscriptionName = "ignorethis";
        private static readonly IDictionary<string, string> resourceTags = new Dictionary<string, string>
        {
            {"ResourceTag", "GeneratedFromTest"},
        };
        private static readonly int PREPARE_TIMEOUT_MINS = 60;
        private static readonly int NUM_STORAGE_TO_CREATE = 1;

        private static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
            return config;
        }

        private static string GetResourceGroupName(Guid groupGuid)
        {
            return azureResourceGroupPrefix + groupGuid.ToString("N") + '-' + Guid.NewGuid().ToString("N");
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
                        null)
                });
            return catalogMoq;
        }

        private static async Task<string> GetSrcBlobUrlAsync(IServicePrincipal servicePrincipal)
        {
            // Get azure client
            var catalogMoq = GetMockSystemCatalog(servicePrincipal);
            var azureClientFactory = new AzureClientFactory(catalogMoq.Object);
            var azure = await azureClientFactory.GetAzureClientAsync(new Guid(azureSubscriptionId));

            // Get storage account key
            var storageAccountsInSubscription = await azure.StorageAccounts.ListAsync();
            var srcStorageAccount = storageAccountsInSubscription.Single(sa => sa.Name == fileShareTemplateStorageAccount);
            var srcStorageAccountKey = (await srcStorageAccount.GetKeysAsync())[0].Value;

            // Create blob sas url
            var storageCreds = new StorageCredentials(srcStorageAccount.Name, srcStorageAccountKey);
            var blobRef = new CloudStorageAccount(storageCreds, useHttps: true)
                .CreateCloudBlobClient()
                .GetContainerReference(fileShareTemplateContainerName)
                .GetBlobReference(fileShareTemplateBlobName);
            var blobSas = blobRef.GetSharedAccessSignature(new SharedAccessBlobPolicy {
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
        public async Task FileShareProviderHelper_E2EFlow_Completes_Without_Errors()
        {
            var logger = new DefaultLoggerFactory().New();

            var resourceGroupTestGroupGuid = Guid.NewGuid();
            var servicePrincipal = GetServicePrincipal();
            var catalogMoq = GetMockSystemCatalog(servicePrincipal);
            var srcBlobUrl = await GetSrcBlobUrlAsync(servicePrincipal);

            // construct the real StorageFileShareProviderHelper
            IStorageFileShareProviderHelper providerHelper = new StorageFileShareProviderHelper(catalogMoq.Object);

            // Create storage accounts
            var storageAccounts = await Task.WhenAll(
                Enumerable.Range(0, NUM_STORAGE_TO_CREATE)
                    .Select(x => providerHelper.CreateStorageAccountAsync(azureSubscriptionId, azureLocationStr,  GetResourceGroupName(resourceGroupTestGroupGuid), resourceTags, logger))
            );

            try
            {
                // Create file shares
                await Task.WhenAll(storageAccounts.Select(sa => providerHelper.CreateFileShareAsync(sa, logger)));

                // Start file share preparations
                await Task.WhenAll(storageAccounts.Select(sa => providerHelper.StartPrepareFileShareAsync(sa, srcBlobUrl, logger)));
                
                double[] completedPercent  = new double[NUM_STORAGE_TO_CREATE];

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                
                while (completedPercent.Any(x => x != 1) && stopWatch.Elapsed < TimeSpan.FromMinutes(PREPARE_TIMEOUT_MINS))
                {
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                    // Check completion status
                    completedPercent = await Task.WhenAll(storageAccounts.Select(sa => providerHelper.CheckPrepareFileShareAsync(sa, logger)));
                }

                stopWatch.Stop();

                // Verify that none still haven't finished after the timeout
                if (completedPercent.Any(x => x != 1))
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
