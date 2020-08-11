// <copyright file="CosmosDbEnvironmentStateChangeArchiveRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Repository.AzureCosmosDb
{
    /// <summary>
    /// the CosmosDbEnvironmentStateChangeRepository.
    /// </summary>
    [DocumentDbCollectionId(CollectionName)]
    public class CosmosDbEnvironmentStateChangeArchiveRepository : CosmosDbBillingBaseRepository<EnvironmentStateChange>, IEnvironmentStateChangeArchiveRepository
    {
        /// <summary>
        /// The name of the collection.
        /// </summary>
        public const string CollectionName = "environment_state_changes_archived";

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbEnvironmentStateChangeArchiveRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public CosmosDbEnvironmentStateChangeArchiveRepository(
                IOptionsMonitor<DocumentDbCollectionOptions> options,
                IRegionalDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues)
            : base(options, clientProvider, healthProvider, loggerFactory, defaultLogValues)
        {
        }

        /// <inheritdoc/>
        protected override string BuildPartitionKey(EnvironmentStateChange entity)
        {
            return $"{entity.PlanId}_{entity.Time:yyyy_MM}";
        }
    }
}
