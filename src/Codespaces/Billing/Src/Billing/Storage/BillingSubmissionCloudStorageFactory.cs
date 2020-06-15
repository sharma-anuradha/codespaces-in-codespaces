// <copyright file="BillingSubmissionCloudStorageFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Storage;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using queue = Microsoft.Azure.Storage.Auth;
using table = Microsoft.Azure.Cosmos.Table;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// A type that creates cloud storage clients (table/Queue) needed for billing submission for a specific azure location.
    /// </summary>
    public class BillingSubmissionCloudStorageFactory : IBillingSubmissionCloudStorageFactory
    {
        private readonly IControlPlaneAzureResourceAccessor azureResourceAccesor;
        private readonly IDiagnosticsLogger logger;
        private readonly IHealthProvider healthProvider;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IResourceNameBuilder resourceNameBuilder;
        private readonly LogValueSet logValues;
        private readonly ConcurrentDictionary<AzureLocation, BillingSubmissionCloudStorageClient> storedCollections;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingSubmissionCloudStorageFactory"/> class.
        /// </summary>
        /// <param name="azureResourceAccesor">Access to control plane resources.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">Ised to generate a logger.</param>
        /// <param name="resourceNameBuilder">Used to generate a lease name.</param>
        /// <param name="logValues">Initial log values.</param>
        public BillingSubmissionCloudStorageFactory(
            IControlPlaneAzureResourceAccessor azureResourceAccesor,
            IDiagnosticsLogger logger,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            IResourceNameBuilder resourceNameBuilder,
            LogValueSet logValues)
        {
            storedCollections = new ConcurrentDictionary<AzureLocation, BillingSubmissionCloudStorageClient>();
            this.azureResourceAccesor = azureResourceAccesor;
            this.logger = logger;
            this.healthProvider = healthProvider;
            this.loggerFactory = loggerFactory;
            this.resourceNameBuilder = resourceNameBuilder;
            this.logValues = logValues;
        }

        /// <inheritdoc />
        public async Task<IBillingSubmissionCloudStorageClient> CreateBillingSubmissionCloudStorage(AzureLocation location)
        {
            // Get the cached collection if one exists
            if (storedCollections.ContainsKey(location))
            {
                return storedCollections[location];
            }
            else
            {
                // Otherwise, pull the billing storage account info and create the desired cloud clients
                var (storageAccountName, storageAccountKey) = await azureResourceAccesor.GetStampStorageAccountForBillingSubmission(location);
                var storageCredentialsForTable = new table.StorageCredentials(storageAccountName, storageAccountKey);
                var storageCredentialsForQueue = new queue.StorageCredentials(storageAccountName, storageAccountKey);

                // Create these two storage accounts... Not sure why these are not unified?
                // TODO: Choose the right unifying API if one exists?
                var storageAccountTable = new table.CloudStorageAccount(storageCredentialsForTable, useHttps: true);
                var storageAccountQueue = new Microsoft.Azure.Storage.CloudStorageAccount(storageCredentialsForQueue, useHttps: true);

                // Create the various clients
                var queueClient = new CloudQueueClient(storageAccountQueue.QueueStorageUri, storageCredentialsForQueue);
                var tableClient = new CloudTableClient(storageAccountTable.TableStorageUri, storageCredentialsForTable);

                var provider = new StorageQueueClientProvider(queueClient);

                var usageCollection = new BillingSubmissionQueueCollection(provider, healthProvider, loggerFactory, resourceNameBuilder, logValues);
                var errorCollection = new BillingSubmissionErrorQueueCollection(provider, healthProvider, loggerFactory, resourceNameBuilder, logValues);

                // cache the collection
                var storageclient = new BillingSubmissionCloudStorageClient(tableClient, usageCollection, errorCollection, logger);
                storedCollections.GetOrAdd(location, storageclient);
                return storageclient;
            }
        }
    }
}
