// <copyright file="StorageFileShareProviderHelperTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Test
{
    public class StorageFileShareProviderHelperTests
    {

        // Note: These are values to test in dev subscription
        private static readonly string azureSubscriptionId = "86642df6-843e-4610-a956-fdd497102261";
        private static readonly string azureSubscriptionName = "vsclk-test";

        private static readonly string azureLocationStr = "westus2";
        private static readonly AzureLocation azureLocation = AzureLocation.WestUs2;

        private static readonly string azureResourceGroup = "vsclk-core-dev-test";

        // Note: Before running tests,
        // Get a Blob SAS URL for https://vsengsaas.blob.core.windows.net/cloudenv-storage-ext4/cloudenvdata_latest
        // It's a private blob so needs SAS token.
        // The test will fail otherwise.
        private static readonly string srcBlobUrl = null;
        private static readonly IServicePrincipal servicePrincipal = null;

        /// <summary>
        /// Run all the operations exposed by the provider helper.
        /// These tests aren't mocked and so run against real Azure.
        /// It can take 20+ minutes for the file share preparation to complete.
        /// </summary>
        [Fact]
        public async Task FileShareProviderHelper_E2EFlow_Completes_Without_Errors()
        {
            Assert.NotNull(srcBlobUrl);

            var catalogMoq = new Mock<ISystemCatalog>();

            catalogMoq
                .Setup(x => x.AzureSubscriptionCatalog.AzureSubscriptions)
                .Returns(new[] {
                    new AzureSubscription(
                        azureSubscriptionId,
                        azureSubscriptionName,
                        servicePrincipal,
                        true,
                        new[] {azureLocation})
                });

            // construct the real StorageFileShareProviderHelper
            IStorageFileShareProviderHelper providerHelper = new StorageFileShareProviderHelper(catalogMoq.Object);

            // Verify that we can create the storage resource and prepare it
            var storageAccountId = await providerHelper.CreateStorageAccountAsync(
                azureSubscriptionId, 
                azureLocationStr, 
                azureResourceGroup);
                
            try
            {
                Assert.NotNull(storageAccountId);

                await providerHelper.CreateFileShareAsync(storageAccountId);
                await providerHelper.StartPrepareFileShareAsync(storageAccountId, srcBlobUrl);
                
                double completedPercent  = 0;
                int prepareTimeoutMinutes = 30;

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                while (completedPercent != 1 && stopWatch.Elapsed < TimeSpan.FromMinutes(prepareTimeoutMinutes))
                {
                    completedPercent = await providerHelper.CheckPrepareFileShareAsync(storageAccountId);
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                }

                stopWatch.Stop();

                // Verify that we can get the connection info
                if (completedPercent == 1)
                {
                    var connInfo = await providerHelper.GetConnectionInfoAsync(storageAccountId);
                    Assert.NotNull(connInfo);
                }
                else
                {
                    Assert.True(false, string.Format("Failed to complete file share preparation in given time of {0} minutes.", prepareTimeoutMinutes));
                }
            }
            finally
            {
                // Verify that we can delete the storage account
                await providerHelper.DeleteStorageAccountAsync(storageAccountId);
            }
        }
    }
}
