// <copyright file="CosmosDbResourceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.CosmosDb
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
        public const string CollectionName = "Resources";

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
            : base(PromoteToOptionSnapshot(options), clientProvider, healthProvider, loggerFactory, defaultLogValues)
        {
        }

        // TEMP: Map backend common and frontend commin into src/Common/Src/Common!
        private static IOptionsSnapshot<TOptions> PromoteToOptionSnapshot<TOptions>(IOptions<TOptions> option)
            where TOptions : class, new()
        {
            return new DirectOptionsSnapshot<TOptions>(option.Value);
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
            var items = await QueryAsync(
                x => x.Where(p => p.SkuName == skuName
                        && p.Type == type
                        && p.Location == location
                        && p.IsAssigned == false)
                    .OrderBy(p => p.Created)
                    .Take(1)
                    .AsDocumentQuery(), logger);

            return items?.FirstOrDefault();
        }
    }
}
