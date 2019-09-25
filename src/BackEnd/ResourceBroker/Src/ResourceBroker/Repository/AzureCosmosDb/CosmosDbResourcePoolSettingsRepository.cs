// <copyright file="CosmosDbResourcePoolSettingsRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.AzureCosmosDb
{
    /// <summary>
    /// A document repository of <see cref="CosmosDbResourcePoolSettingsRepository"/>.
    /// </summary>
    [DocumentDbCollectionId(CollectionName)]
    public class CosmosDbResourcePoolSettingsRepository : DocumentDbCollection<ResourcePoolSettingsRecord>, IResourcePoolSettingsRepository
    {
        /// <summary>
        /// The name of the collection in CosmosDB.
        /// </summary>
        public const string CollectionName = "resources-pool-settings";

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbResourcePoolSettingsRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public CosmosDbResourcePoolSettingsRepository(
                IOptions<DocumentDbCollectionOptions> options,
                IDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues)
            : base(PromoteToOptionSnapshot(options.Value), clientProvider, healthProvider, loggerFactory, defaultLogValues)
        {
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

        // TEMP: Map backend common and frontend common into src/Common/Src/Common!
        private static IOptionsSnapshot<TOptions> PromoteToOptionSnapshot<TOptions>(TOptions option)
            where TOptions : class, new()
        {
            ConfigureOptions(option as DocumentDbCollectionOptions);
            return new DirectOptionsSnapshot<TOptions>(option);
        }

        // TEMP: Map backend common and frontend common into src/Common/Src/Common!
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
    }
}
