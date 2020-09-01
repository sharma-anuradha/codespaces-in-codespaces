// <copyright file="CosmosDbEnvironmentStateChangeRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    [DocumentDbCollectionId(EventCollectionId)]
    public class CosmosDbEnvironmentStateChangeRepository : CosmosDbBillingBaseRepository<EnvironmentStateChange>, IEnvironmentStateChangeRepository
    {
        /// <summary>
        /// The name of the collection.
        /// </summary>
        public const string EventCollectionId = "environment_state_changes";

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbEnvironmentStateChangeRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public CosmosDbEnvironmentStateChangeRepository(
                IOptionsMonitor<DocumentDbCollectionOptions> options,
                IRegionalDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues)
            : base(options, clientProvider, healthProvider, loggerFactory, defaultLogValues)
        {
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<EnvironmentStateChange>> GetAllEnvironmentEventsAsync(string planId, DateTime endTime, IDiagnosticsLogger logger)
        {
            return await QueryAsync(
                q =>
                q.Where(x => x.PartitionKey == planId && x.Time <= endTime),
                logger);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<EnvironmentStateChange>> GetAllEnvironmentEventsAsync(string planId, DateTime startTime, DateTime endTime, IDiagnosticsLogger logger)
        {
            return await QueryAsync(
                q =>
                q.Where(x => x.PartitionKey == planId &&
                    x.Time >= startTime &&
                    x.Time < endTime),
                logger);
        }

        /// <inheritdoc/>
        protected override string BuildPartitionKey(EnvironmentStateChange entity)
        {
            return EnvironmentStateChange.CreateActivePartitionKey(entity.PlanId);
        }
    }
}
