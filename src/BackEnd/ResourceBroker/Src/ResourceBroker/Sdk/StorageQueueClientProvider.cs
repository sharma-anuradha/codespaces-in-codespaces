// <copyright file="StorageQueueClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <inheritdoc/>
    public class StorageQueueClientProvider : IStorageQueueClientProvider
    {
        private readonly Task<CloudQueueClient> cloudQueueClientTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageQueueClientProvider"/> class.
        /// </summary>
        /// <param name="controlPlaneAzureResourceAccessor">The control plane azure resource accessor.</param>
        public StorageQueueClientProvider(
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
        {
            Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));
            cloudQueueClientTask = InitQueueClient(controlPlaneAzureResourceAccessor);
        }

        /// <inheritdoc/>
        public async Task<CloudQueueClient> GetQueueClientAsync()
        {
            return await cloudQueueClientTask;
        }

        /// <inheritdoc/>
        public async Task<CloudQueue> GetQueueAsync([ValidatedNotNull] string queueName)
        {
            Requires.NotNullOrEmpty(queueName, nameof(queueName));

            var queueClient = await GetQueueClientAsync();
            var client = queueClient.GetQueueReference(queueName);

            await client.CreateIfNotExistsAsync();

            return client;
        }

        private async Task<CloudQueueClient> InitQueueClient(IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
        {
            var (accountName, accountKey) = await controlPlaneAzureResourceAccessor.GetStampStorageAccountAsync();
            var storageCredentials = new StorageCredentials(accountName, accountKey);
            var storageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
            var queueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageCredentials);
            return queueClient;
        }
    }
}
