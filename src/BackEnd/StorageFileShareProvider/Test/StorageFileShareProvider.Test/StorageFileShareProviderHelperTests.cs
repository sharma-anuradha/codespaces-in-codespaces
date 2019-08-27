// <copyright file="StorageFileShareProviderHelperTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
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
        private static readonly int PREPARE_TIMEOUT_MINS = 60;
        private static readonly int NUM_STORAGE_TO_CREATE = 1;

        // Note: Before running tests,
        // Get a Blob SAS URL.
        // If it's a private blob include the SAS token.
        // The test will fail otherwise.
        private static readonly string srcBlobUrl = "";
        private static readonly IServicePrincipal servicePrincipal = null;

        /// <summary>
        /// Run all the operations exposed by the provider helper.
        /// These tests aren't mocked and so run against real Azure.
        /// It can take 20+ minutes for the file share preparation to complete.
        /// </summary>
        [Fact(Skip = "integration test")]
        public async Task FileShareProviderHelper_E2EFlow_Completes_Without_Errors()
        {
            Assert.NotNull(srcBlobUrl);

            var logger = new DefaultLoggerFactory().New();

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

            // Create storage accounts
            var storageAccountIds = await Task.WhenAll(Enumerable.Range(0, NUM_STORAGE_TO_CREATE).Select(x => providerHelper.CreateStorageAccountAsync(azureSubscriptionId, azureLocationStr, azureResourceGroup, logger)));

            try
            {
                // Create file shares
                await Task.WhenAll(storageAccountIds.Select(id => providerHelper.CreateFileShareAsync(id, logger)));

                // Start file share preparations
                await Task.WhenAll(storageAccountIds.Select(id => providerHelper.StartPrepareFileShareAsync(id, srcBlobUrl, logger)));
                
                double[] completedPercent  = new double[NUM_STORAGE_TO_CREATE];

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                
                while (completedPercent.Any(x => x != 1) && stopWatch.Elapsed < TimeSpan.FromMinutes(PREPARE_TIMEOUT_MINS))
                {
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                    // Check completion status
                    completedPercent = await Task.WhenAll(storageAccountIds.Select(id => providerHelper.CheckPrepareFileShareAsync(id, logger)));
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
                await Task.WhenAll(storageAccountIds.Select(id => providerHelper.DeleteStorageAccountAsync(id, logger)));
            }
        }
    }
}
