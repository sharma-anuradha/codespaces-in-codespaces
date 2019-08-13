// <copyright file="StorageQueueClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <inheritdoc/>
    public class StorageQueueClientProvider : IStorageQueueClientProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageQueueClientProvider"/> class.
        /// </summary>
        /// <param name="storageAccountSettings">The <see cref="StorageAccountSettings"/> options instance.</param>
        public StorageQueueClientProvider(
            StorageAccountSettings storageAccountSettings)
        {
            Requires.NotNull(storageAccountSettings, nameof(storageAccountSettings));

            var storageCredentials = new StorageCredentials(
                storageAccountSettings.StorageAccountName,
                storageAccountSettings.StorageAccountKey);

            var storageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);

            QueueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageCredentials);
        }

        /// <inheritdoc/>
        public CloudQueueClient QueueClient { get; }

        /// <inheritdoc/>
        public async Task<CloudQueue> GetQueueAsync([ValidatedNotNull] string queueName)
        {
            Requires.NotNullOrEmpty(queueName, nameof(queueName));

            var client = QueueClient.GetQueueReference(queueName);

            await client.CreateIfNotExistsAsync();

            return client;
        }
    }
}
