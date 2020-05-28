// <copyright file="PlanRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <summary>
    /// The Azure VSO Plan repository.
    /// </summary>
    [DocumentDbCollectionId(PlanCollectionId)]
    public class PlanRepository : DocumentDbCollection<VsoPlan>, IPlanRepository
    {
        /// <summary>
        /// The plans collection id.
        /// </summary>
        public const string PlanCollectionId = "environment_billing_plans";

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanRepository"/> class.
        /// </summary>
        /// <param name="collectionOptions">The collection options.</param>
        /// <param name="clientProvider">The doc db client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The diagnostics logging factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public PlanRepository(
            IOptionsMonitor<DocumentDbCollectionOptions> collectionOptions,
            IDocumentDbClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(
                collectionOptions,
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

        /// <inheritdoc/>
        public async Task<IEnumerable<VsoPlan>> GetBillablePlansByShardAsync(string planShard, TimeSpan pagingDelay, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT * FROM c
                    WHERE (c.isFinalBillSubmitted != true OR NOT IS_DEFINED(c.isFinalBillSubmitted))
                    AND STARTSWITH(c.plan.subscription, @planShard)",
                new SqlParameterCollection
                {
                        new SqlParameter { Name = "@planShard", Value = planShard },
                });

            var plans = await QueryAsync(
                (client, uri, feedOptions) => client.CreateDocumentQuery<VsoPlan>(uri, query, feedOptions).AsDocumentQuery(),
                logger,
                (_, childlogger) =>
                {
                    return Task.Delay(pagingDelay);
                });

            return plans;
        }
    }
}
