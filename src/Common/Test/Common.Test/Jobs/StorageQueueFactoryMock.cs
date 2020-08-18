using System;
using System.Collections.Generic;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Moq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Test
{
    internal static class StorageQueueFactoryMock
    {
        const string AccountName = "vsclkonlinedevciusw2sa";
        const string AccountKey = "{}";

        const string AccountName_WestEurope = "vsclkonlinedevcieuwsa";
        const string AccountKey_WestEurope = "{}";

        public static IQueueFactory Create()
        {
            var mockStorageQueueClientProvider = new Mock<IStorageQueueClientProvider>();
            mockStorageQueueClientProvider.Setup(e => e.GetQueueAsync(It.IsAny<string>()))
                        .Returns(async (string queueName) =>
                        {
                            var cloudQueueClient = CreateCloudQueueClient(AccountName, AccountKey);
                            var client = cloudQueueClient.GetQueueReference(queueName);

                            await client.CreateIfNotExistsAsync();
                            return client;
                        });

            var mockCrossRegionStorageQueueClientProvider = new Mock<ICrossRegionStorageQueueClientProvider>();
            mockCrossRegionStorageQueueClientProvider.Setup(e => e.GetQueueAsync(It.IsAny<string>(), It.IsAny<AzureLocation>()))
                        .Returns(async (string queueName, AzureLocation controlPlaneRegion) =>
                        {
                            var cloudQueueClient = CreateCloudQueueClient(AccountName_WestEurope, AccountKey_WestEurope);
                            var client = cloudQueueClient.GetQueueReference(queueName);

                            await client.CreateIfNotExistsAsync();
                            return client;
                        });

            var mockControlPlaneInfo = new Mock<IControlPlaneInfo>();
            mockControlPlaneInfo.SetupGet(x => x.AllStamps).Returns(() =>
                new Dictionary<AzureLocation, IControlPlaneStampInfo>()
                { { AzureLocation.WestEurope, null } });

            var mockHealthProvider = new Mock<IHealthProvider>();
            mockHealthProvider.Setup(e => e.MarkUnhealthy(It.IsAny<Exception>(), It.IsAny<IDiagnosticsLogger>()))
                .Verifiable();
            return new StorageQueueFactory(
                mockStorageQueueClientProvider.Object,
                mockCrossRegionStorageQueueClientProvider.Object,
                mockControlPlaneInfo.Object,
                new ResourceNameBuilder(new DeveloperPersonalStampSettings(true, string.Empty, true)),
                mockHealthProvider.Object,
                new NullLogger());
        }

        private static CloudQueueClient CreateCloudQueueClient(string accountName, string accountKey)
        {
            var storageCredentials = new StorageCredentials(accountName, accountKey);
            var storageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
            var queueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageCredentials);
            return queueClient;
        }
    }
}
