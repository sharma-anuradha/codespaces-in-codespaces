// <copyright file="CosmosDbBillSummaryArchiveRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Repository.AzureCosmosDb
{
    /// <summary>
    /// The CosmosDB archived bill summary repository.
    /// </summary>
    [DocumentDbCollectionId(CollectionName)]
    public class CosmosDbBillSummaryArchiveRepository : CosmosDbBillingBaseRepository<BillSummary>, IBillSummaryArchiveRepository
    {
        /// <summary>
        /// The name of the collection.
        /// </summary>
        public const string CollectionName = "bill_summaries_archived";

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbBillSummaryArchiveRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public CosmosDbBillSummaryArchiveRepository(
                IOptionsMonitor<DocumentDbCollectionOptions> options,
                IRegionalDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues)
            : base(options, clientProvider, healthProvider, loggerFactory, defaultLogValues)
        {
        }

        /// <inheritdoc/>
        protected override string BuildPartitionKey(BillSummary entity)
        {
            return BillSummary.CreateArchivedPartitionKey(entity.PlanId, entity.BillGenerationTime);
        }
    }
}
