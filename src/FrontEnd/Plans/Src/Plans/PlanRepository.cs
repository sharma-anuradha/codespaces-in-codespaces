// <copyright file="PlanRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    [DocumentDbCollectionId(PlanCollectionId)]
    public class PlanRepository : DocumentDbCollection<VsoPlan>, IPlanRepository
    {
        public const string PlanCollectionId = "environment_billing_plans";

        public PlanRepository(
            IOptions<DocumentDbCollectionOptions> collectionOptions,
            IDocumentDbClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(
                new DocumentDbCollectionOptionsSnapshot(collectionOptions, ConfigureOptions),
                clientProvider,
                healthProvider,
                loggerFactory,
                defaultLogValues)
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
                "/plan/subscription",
            };
            options.CustomPartitionKeyFunc = (entity) =>
            {
                return new PartitionKey(((VsoPlan)entity).Plan?.Subscription);
            };
        }

        /// <inheritdoc/>
        public async Task<int> GetCountAsync(IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(@"SELECT VALUE COUNT(1) FROM c");

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<int>(uri, query, feedOptions).AsDocumentQuery(), logger);

            var count = items.FirstOrDefault();

            return count;
        }

        /// <inheritdoc/>
        public async Task<int> GetPlanSubscriptionCountAsync(IDiagnosticsLogger logger)
        {
            // TODO: This query is a bit more expensive than we would like. We should fix all the older isDeleted to is False so that we do not have to run with an Is_defined which is a bit more expensive than we would like.
            var query = new SqlQuerySpec(@"SELECT VALUE SUM(1) FROM (
                                            SELECT DISTINCT VALUE c.plan.subscription from c where c.isDeleted != true or not IS_DEFINED(c.isDeleted)) d");
            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<int>(uri, query, feedOptions).AsDocumentQuery(), logger);
            var count = items.FirstOrDefault();

            return count;
        }
    }
}
