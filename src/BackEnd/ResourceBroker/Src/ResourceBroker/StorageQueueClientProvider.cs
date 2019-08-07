// <copyright file="StorageQueueClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <inheritdoc/>
    public class StorageQueueClientProvider : IStorageQueueClientProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageQueueClientProvider"/> class.
        /// </summary>
        /// <param name="options">The <see cref="StorageAccountSettings"/> options instance.</param>
        public StorageQueueClientProvider(
            [ValidatedNotNull] IOptions<StorageAccountOptions> options)
        {
            Requires.NotNull(options, nameof(options));
            var settings = options.Value.Settings;

            var storageCredentials = new StorageCredentials(settings.StorageAccountName, settings.StorageAccountKey);
            var storageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
            QueueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageCredentials);
        }

        /// <inheritdoc/>
        public CloudQueueClient QueueClient { get; }

        /// <inheritdoc/>
        public CloudQueue GetQueue([ValidatedNotNull] string queueName)
        {
            Requires.NotNullOrEmpty(queueName, nameof(queueName));
            return QueueClient.GetQueueReference(queueName);
        }
    }
}
