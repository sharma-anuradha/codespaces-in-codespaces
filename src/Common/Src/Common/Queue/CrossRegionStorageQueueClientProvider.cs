// <copyright file="CrossRegionStorageQueueClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <inheritdoc/>
    public class CrossRegionStorageQueueClientProvider : ICrossRegionStorageQueueClientProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CrossRegionStorageQueueClientProvider"/> class.
        /// </summary>
        /// <param name="crossRegionControlPlaneInfo">Target cross region Control plane info.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="logger">Target logger.</param>
        public CrossRegionStorageQueueClientProvider(
            ICrossRegionControlPlaneInfo crossRegionControlPlaneInfo,
            ITaskHelper taskHelper,
            IDiagnosticsLogger logger)
        {
            CloudQueueClientsTask = InitQueueClients(crossRegionControlPlaneInfo, taskHelper, logger);
        }

        private string LogBaseName => "cross_region_storage_queue_client_provider";

        private Task<IReadOnlyDictionary<AzureLocation, CloudQueueClient>> CloudQueueClientsTask { get; }

        /// <inheritdoc/>
        public async Task<CloudQueueClient> GetQueueClientAsync(
            AzureLocation controlPlaneRegion)
        {
            // Find specific client
            var clients = await CloudQueueClientsTask;
            if (clients.TryGetValue(controlPlaneRegion, out var client))
            {
                return client;
            }

            throw new NotSupportedException($"Target region of {controlPlaneRegion} was not found in cache.");
        }

        /// <inheritdoc/>
        public async Task<CloudQueue> GetQueueAsync(
            [ValidatedNotNull] string queueName,
            AzureLocation controlPlaneRegion)
        {
            Requires.NotNullOrEmpty(queueName, nameof(queueName));

            var queueClient = await GetQueueClientAsync(controlPlaneRegion);
            var queue = queueClient.GetQueueReference(queueName);

            await queue.CreateIfNotExistsAsync();

            return queue;
        }

        private async Task<IReadOnlyDictionary<AzureLocation, CloudQueueClient>> InitQueueClients(
            ICrossRegionControlPlaneInfo crossRegionControlPlaneInfo,
            ITaskHelper taskHelper,
            IDiagnosticsLogger logger)
        {
            var clients = new Dictionary<AzureLocation, CloudQueueClient>();

            await taskHelper.RunConcurrentEnumerableAsync(
                $"{LogBaseName}_init_queue_clients",
                crossRegionControlPlaneInfo.AllResourceAccessors,
                async (accessor, childLogger) =>
                {
                    var client = await InitQueueClient(accessor.Value);

                    clients.Add(accessor.Key, client);
                },
                logger,
                concurrentLimit: 4);

            return new ReadOnlyDictionary<AzureLocation, CloudQueueClient>(clients);
        }

        private async Task<CloudQueueClient> InitQueueClient(
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
        {
            var (accountName, accountKey) = await controlPlaneAzureResourceAccessor.GetStampStorageAccountAsync();
            var storageCredentials = new StorageCredentials(accountName, accountKey);
            var storageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
            var queueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageCredentials);
            return queueClient;
        }
    }
}
