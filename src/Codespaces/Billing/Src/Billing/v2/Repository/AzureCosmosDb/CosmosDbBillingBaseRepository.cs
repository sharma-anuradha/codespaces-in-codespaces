// <copyright file="CosmosDbBillingBaseRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// A base repository which automatically handles partition keys.
    /// </summary>
    /// <typeparam name="T">The Cosmos DB Entity type.</typeparam>
    public abstract class CosmosDbBillingBaseRepository<T> : DocumentDbCollection<T>
        where T : CosmosDbEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbBillingBaseRepository{T}"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public CosmosDbBillingBaseRepository(
                IOptionsMonitor<DocumentDbCollectionOptions> options,
                IRegionalDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues)
            : base(options, clientProvider, healthProvider, loggerFactory, defaultLogValues)
        {
        }

        /// <summary>
        /// Configures the standard options for this repository.
        /// </summary>
        /// <param name="options">The options instance.</param>
        public static void ConfigureOptions(DocumentDbCollectionOptions options)
        {
            Requires.NotNull(options, nameof(options));
            options.PartitioningStrategy = PartitioningStrategy.Custom;
            options.CustomPartitionKeyPaths = new[]
            {
                // Partitioning on Subscription ID under the SkuPlan object
                "/partitionKey",
            };
            options.CustomPartitionKeyFunc = (entity) =>
            {
                return new PartitionKey(((T)entity).PartitionKey);
            };
        }

        /// <inheritdoc/>
        public override Task<T> CreateAsync([ValidatedNotNull] T document, [ValidatedNotNull] IDiagnosticsLogger logger)
        {
            document.PartitionKey = BuildPartitionKey(document);

            return base.CreateAsync(document, logger);
        }

        /// <inheritdoc/>
        public override Task<T> CreateOrUpdateAsync([ValidatedNotNull] T document, [ValidatedNotNull] IDiagnosticsLogger logger)
        {
            document.PartitionKey = BuildPartitionKey(document);

            return base.CreateOrUpdateAsync(document, logger);
        }

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="document">Entity to delete.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>Task.</returns>
        public Task<bool> DeleteAsync([ValidatedNotNull] T document, [ValidatedNotNull] IDiagnosticsLogger logger)
        {
            var key = new DocumentDbKey(document.Id, new PartitionKey(document.PartitionKey));

            return DeleteAsync(key, logger);
        }

        /// <summary>
        /// Builds the partition key for the given entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The key.</returns>
        protected abstract string BuildPartitionKey(T entity);
    }
}
