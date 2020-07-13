using System;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Moq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Test
{
    internal static class StorageQueueFactoryMock
    {
        const string AccountName = "{}";
        const string AccountKey = "{}";

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

            var mockHealthProvider = new Mock<IHealthProvider>();
            mockHealthProvider.Setup(e => e.MarkUnhealthy(It.IsAny<Exception>(), It.IsAny<IDiagnosticsLogger>()))
                .Verifiable();
            return new StorageQueueFactory(
                mockStorageQueueClientProvider.Object,
                mockHealthProvider.Object,
                new NullDiagnosticsLoggerFactory(),
                new ResourceNameBuilder(new DeveloperPersonalStampSettings(true, string.Empty, true)),
                new LogValueSet());
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
