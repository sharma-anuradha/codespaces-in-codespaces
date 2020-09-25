// <copyright file="SecretStoreRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Repository
{
    /// <summary>
    /// User/Plan secrets store repository.
    /// </summary>
    [DocumentDbCollectionId(SecretStoreCollectionId)]
    public class SecretStoreRepository : DocumentDbCollection<SecretStore>, ISecretStoreRepository
    {
        /// <summary>
        /// The plans secrets collection id.
        /// </summary>
        public const string SecretStoreCollectionId = "secret_store";

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretStoreRepository"/> class.
        /// </summary>
        /// <param name="collectionOptions">The collection options.</param>
        /// <param name="clientProvider">The doc db client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The diagnostics logging factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public SecretStoreRepository(
            IOptionsMonitor<DocumentDbCollectionOptions> collectionOptions,
            IDocumentDbClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(collectionOptions, clientProvider, healthProvider, loggerFactory, defaultLogValues)
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
                "/planId",
            };
            options.CustomPartitionKeyFunc = (entity) =>
            {
                return new PartitionKey(((SecretStore)entity).PlanId);
            };
        }

        /// <inheritdoc/>
        public async Task<SecretStore> GetSecretStoreByOwnerAndPlanAsync(
            SecretScope secretScope,
            string ownerId,
            string planId,
            IDiagnosticsLogger logger)
        {
            var secretsStores = await GetWhereAsync(
                (secretsStore) =>
                    secretsStore.Scope == secretScope &&
                    secretsStore.OwnerId == ownerId &&
                    secretsStore.PlanId == planId,
                logger);

            return secretsStores.SingleOrDefault();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SecretStore>> GetAllPlanSecretStoresByUserAsync(
            string userId,
            string planId,
            IDiagnosticsLogger logger)
        {
            return await GetWhereAsync(
               (secretsStore) =>
                   (secretsStore.PlanId == planId) &&
                   ((secretsStore.Scope == SecretScope.Plan) ||
                   (secretsStore.Scope == SecretScope.User && secretsStore.OwnerId == userId)),
               logger);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SecretStore>> GetSecretStoresByPlanIdAsync(
            string planId,
            IDiagnosticsLogger logger)
        {
            return await GetWhereAsync(
                (secretStore) =>
                    (secretStore.PlanId == planId),
                logger);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(string id, string planId, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(id, nameof(id));
            Requires.NotNullOrEmpty(planId, nameof(planId));

            var documentDbKey = ConstructDocumentDbKey(id, planId);
            return await DeleteAsync(documentDbKey, logger);
        }

        /// <inheritdoc/>
        public async Task<SecretStore> GetAsync(string id, string planId, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(id, nameof(id));
            Requires.NotNullOrEmpty(planId, nameof(planId));

            var documentDbKey = ConstructDocumentDbKey(id, planId);
            return await GetAsync(documentDbKey, logger);
        }

        public async Task<SecretStore> GetSecretStoreUsingResource(string resourceId, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT TOP @count VALUE c
                FROM c
                WHERE c.secretResource != null and  c.secretResource.resourceId = @targetResourceId",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@count", Value = 1 },
                    new SqlParameter { Name = "@targetResourceId", Value = resourceId },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<SecretStore>(uri, query, feedOptions).AsDocumentQuery(), logger.NewChildLogger());
            return items.FirstOrDefault();
        }

        private DocumentDbKey ConstructDocumentDbKey(string id, string planId)
        {
            var partitionKey = new PartitionKey(planId);
            return new DocumentDbKey(id, partitionKey);
        }
    }
}
