// <copyright file="CosmosDbBillSummaryRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Repository.AzureCosmosDb
{
    /// <summary>
    /// The cosmosDB summary repository.
    /// </summary>
    [DocumentDbCollectionId(EventCollectionId)]
    public class CosmosDbBillSummaryRepository : CosmosDbBillingBaseRepository<BillSummary>, IBillSummaryRepository
    {
        /// <summary>
        /// The name of the collection.
        /// </summary>
        public const string EventCollectionId = "bill_summaries";

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbBillSummaryRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public CosmosDbBillSummaryRepository(
                IOptionsMonitor<DocumentDbCollectionOptions> options,
                IRegionalDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues)
            : base(options, clientProvider, healthProvider, loggerFactory, defaultLogValues)
        {
        }

        /// <inheritdoc/>
        public Task<IEnumerable<BillSummary>> GetAllAsync(string planId, DateTime endTime, IDiagnosticsLogger logger)
        {
            return QueryAsync(
                q =>
                q.Where(x => x.PartitionKey == planId && x.PeriodEnd <= endTime),
                logger);
        }

        /// <inheritdoc />
        public async Task<BillSummary> GetLatestAsync(string planId, IDiagnosticsLogger logger)
        {
            // TODO: look based on most recent ID
            return (await this.QueryAsync(
                q =>
                    q.Where(x => x.PartitionKey == planId)
                    .OrderByDescending(x => x.BillGenerationTime)
                    .Take(1), logger))
                    .SingleOrDefault();
        }

        /// <inheritdoc/>
        protected override string BuildPartitionKey(BillSummary entity)
        {
            return entity.PlanId;
        }
    }
}
