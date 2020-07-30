// <copyright file="PartnerCloudStorageFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using queue = Microsoft.Azure.Storage.Auth;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// A type that creates cloud storage clients (table/Queue) needed for partner submission for a specific azure location.
    /// </summary>
    /// <typeparam name="T">asdf.</typeparam>
    public class PartnerCloudStorageFactory<T> : IPartnerCloudStorageFactory
        where T : StorageQueueCollection
    {
        private readonly IControlPlaneAzureResourceAccessor azureResourceAccesor;
        private readonly IDiagnosticsLogger logger;
        private readonly IHealthProvider healthProvider;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IResourceNameBuilder resourceNameBuilder;
        private readonly LogValueSet logValues;
        private readonly ConcurrentDictionary<AzureLocation, PartnerCloudStorageClient> storedCollections;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartnerCloudStorageFactory{T}"/> class.
        /// </summary>
        /// <param name="azureResourceAccesor">Access to control plane resources.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">Ised to generate a logger.</param>
        /// <param name="resourceNameBuilder">Used to generate a lease name.</param>
        /// <param name="logValues">Initial log values.</param>
        public PartnerCloudStorageFactory(
            IControlPlaneAzureResourceAccessor azureResourceAccesor,
            IDiagnosticsLogger logger,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            IResourceNameBuilder resourceNameBuilder,
            LogValueSet logValues)
        {
            this.storedCollections = new ConcurrentDictionary<AzureLocation, PartnerCloudStorageClient>();
            this.azureResourceAccesor = azureResourceAccesor;
            this.logger = logger;
            this.healthProvider = healthProvider;
            this.loggerFactory = loggerFactory;
            this.resourceNameBuilder = resourceNameBuilder;
            this.logValues = logValues;
        }

        /// <inheritdoc />
        public async Task<IPartnerCloudStorageClient> CreatePartnerCloudStorage(AzureLocation location, string partnerId)
        {
            // Get the cached collection if one exists
            if (this.storedCollections.ContainsKey(location))
            {
                return this.storedCollections[location];
            }
            else
            {
                // Create the partner queue
                var (storageAccountName, storageAccountKey) = await this.azureResourceAccesor.GetStampStorageAccountForPartner(location, partnerId);
                var credentials = new queue.StorageCredentials(storageAccountName, storageAccountKey);
                var storageAccount = new Microsoft.Azure.Storage.CloudStorageAccount(credentials, useHttps: true);
                var client = new CloudQueueClient(storageAccount.QueueStorageUri, credentials);
                var provider = new StorageQueueClientProvider(client);
                var queue = (StorageQueueCollection)Activator.CreateInstance(
                    typeof(T), provider, this.healthProvider, this.loggerFactory, this.resourceNameBuilder, this.logValues);
                var storageclient = new PartnerCloudStorageClient(queue, this.logger);

                this.storedCollections.GetOrAdd(location, storageclient);

                return storageclient;
            }
        }
    }
}
