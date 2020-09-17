// <copyright file="CloudEnvironmentCosmosContainer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Cosmos;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository
{
    /// <summary>
    /// A Cosmos DB container of <see cref="CloudEnvironment"/>.
    /// </summary>
    [ContainerId(DocumentDbCloudEnvironmentRepository.CloudEnvironmentsCollectionId)]
    public class CloudEnvironmentCosmosContainer : CosmosContainer<CloudEnvironment>, ICloudEnvironmentCosmosContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentCosmosContainer"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="clientProvider">The cosmos db client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="diagnosticsLoggerFactory">The diagnostics logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public CloudEnvironmentCosmosContainer(
            IOptionsMonitor<CosmosContainerOptions> options,
            ICosmosClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory diagnosticsLoggerFactory,
            LogValueSet defaultLogValues)
            : base(options, clientProvider, healthProvider, diagnosticsLoggerFactory, defaultLogValues)
        {
        }

        /// <summary>
        /// Configures the standard options for this repository.
        /// </summary>
        /// <param name="options">The options instance.</param>
        /// <remarks>
        /// Keep this in sync with <see cref="DocumentDbCloudEnvironmentRepository.ConfigureOptions"/>.
        /// </remarks>
        public static void ConfigureOptions(CosmosContainerOptions options)
        {
            Requires.NotNull(options, nameof(options));
            options.SetPartitioningStrategy(PartitioningStrategy.IdOnly);
        }

        /// <summary>
        /// Get environment count by dimensions.
        /// </summary>
        /// <param name="location">The azure location to query.</param>
        /// <param name="logger">The diganostics logger.</param>
        /// <returns>Query results.</returns>
        public async Task<QueryResults<CloudEnvironmentCountByDimensions>> GetCountByDimensionsAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            var queryDefinition = new QueryDefinition(
                @"SELECT COUNT(1) AS count, c.skuName, c.location, c.state, c.partner, c.creationMetrics.isoCountryCode, c.creationMetrics.azureGeography
                    FROM c
                    WHERE c.location = @location AND (NOT c.isDeleted OR NOT IS_DEFINED(c.isDeleted))
                    GROUP BY c.skuName, c.location, c.state, c.partner, c.creationMetrics.isoCountryCode, c.creationMetrics.azureGeography")
                .WithParameter("@location", location.ToString());

            var queryResults = await QueryAsync<CloudEnvironmentCountByDimensions>(nameof(GetCountByDimensionsAsync), queryDefinition, logger);

            return queryResults;
        }
    }
}
