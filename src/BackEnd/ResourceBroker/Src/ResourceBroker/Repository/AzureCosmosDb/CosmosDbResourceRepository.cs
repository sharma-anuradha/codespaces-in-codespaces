// <copyright file="CosmosDbResourceRepository.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.AzureCosmosDb
{
    /// <summary>
    /// A document repository of <see cref="ExampleEntity"/>.
    /// </summary>
    [DocumentDbCollectionId(CollectionName)]
    public class CosmosDbResourceRepository : DocumentDbCollection<ResourceRecord>, IResourceRepository
    {
        /// <summary>
        /// The name of the collection in CosmosDB.
        /// </summary>
        public const string CollectionName = "resources";

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbResourceRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public CosmosDbResourceRepository(
                IOptions<DocumentDbCollectionOptions> options,
                IDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues)
            : base(PromoteToOptionSnapshot(options.Value), clientProvider, healthProvider, loggerFactory, defaultLogValues)
        {
        }

        // TEMP: Map backend common and frontend commin into src/Common/Src/Common!
        private static IOptionsSnapshot<TOptions> PromoteToOptionSnapshot<TOptions>(TOptions option)
            where TOptions : class, new()
        {
            ConfigureOptions(option as DocumentDbCollectionOptions);
            return new DirectOptionsSnapshot<TOptions>(option);
        }

        // TEMP: Map backend common and frontend commin into src/Common/Src/Common!
        private class DirectOptionsSnapshot<TOptions> : IOptionsSnapshot<TOptions>
            where TOptions : class, new()
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DirectOptionsSnapshot{TOptions}"/> class.
            /// </summary>
            /// <param name="options">The options instance.</param>
            public DirectOptionsSnapshot(TOptions options)
            {
                Options = options;
            }

            /// <summary>
            /// Gets the options value.
            /// </summary>
            public TOptions Value => Options;

            private TOptions Options { get; }

            public TOptions Get(string name)
            {
                return Options;
            }
        }

        /// <summary>
        /// Configures the standard options for this repository.
        /// </summary>
        /// <param name="options">The options instance.</param>
        public static void ConfigureOptions(DocumentDbCollectionOptions options)
        {
            Requires.NotNull(options, nameof(options));
            options.PartitioningStrategy = PartitioningStrategy.IdOnly;
            options.LogPreconditionFailedErrorsAsWarnings = true;
        }

        /// <inheritdoc/>
        public async Task<ResourceRecord> GetUnassignedResourceAsync(
            string skuName, ResourceType type, string location, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT TOP 1 * 
                FROM c
                WHERE c.skuName = @skuName
                    and c.location = @location
                    and c.type = @type
                    and c.isAssigned = @isAssigned
                    and c.isReady = @isReady
                    and c.isDeleted = @isDeleted
                ORDER BY c.ready",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@skuName", Value = skuName },
                    new SqlParameter { Name = "@type", Value = type.ToString() },
                    new SqlParameter { Name = "@location", Value = location.ToLowerInvariant() },
                    new SqlParameter { Name = "@isAssigned", Value = false },
                    new SqlParameter { Name = "@isReady", Value = true },
                    new SqlParameter { Name = "@isDeleted", Value = false },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<ResourceRecord>(uri, query, feedOptions).AsDocumentQuery(), logger);

            var result = items.FirstOrDefault();

            return result;
        }

        /// <inheritdoc/>
        public async Task<int> GetUnassignedCountAsync(string skuName, ResourceType type, string location, IDiagnosticsLogger logger)
        {
            var query = new SqlQuerySpec(
                @"SELECT VALUE COUNT(1) 
                FROM c 
                WHERE c.skuName = @skuName
                    AND c.type = @type
                    AND c.location = @location
                    AND c.isAssigned = @isAssigned
                    AND c.isDeleted = @isDeleted
                    AND c.provisioningStatus != @provisioningStatus",
                new SqlParameterCollection
                {
                    new SqlParameter { Name = "@skuName", Value = skuName },
                    new SqlParameter { Name = "@type", Value = type.ToString() },
                    new SqlParameter { Name = "@location", Value = location.ToLowerInvariant() },
                    new SqlParameter { Name = "@isAssigned", Value = false },
                    new SqlParameter { Name = "@isDeleted", Value = false },
                    new SqlParameter { Name = "@provisioningStatus", Value = OperationState.Failed },
                });

            var items = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<int>(uri, query, feedOptions).AsDocumentQuery(), logger);

            var count = items.FirstOrDefault();

            return count;
        }
    }
}
