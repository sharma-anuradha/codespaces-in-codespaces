// <copyright file="RegionalSystemConfigurationMigrationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Backend.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks.SystemConfigurationMigration
{
    /// <summary>
    /// A document db collection of <see cref="SystemConfigurationMigrationRecord"/>.
    /// </summary>
    [DocumentDbCollectionId(CollectionName)]
    public class RegionalSystemConfigurationMigrationRepository : DocumentDbCollection<SystemConfigurationMigrationRecord>, IRegionalSystemConfigurationMigrationRepository
    {
        /// <summary>
        /// The cosmos db collection name.
        /// </summary>
        public const string CollectionName = "configuration";

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionalSystemConfigurationMigrationRepository"/> class.
        /// </summary>
        /// <param name="collectionOptions">The collection options.</param>
        /// <param name="clientProvider">The document db client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public RegionalSystemConfigurationMigrationRepository(
            IOptionsMonitor<DocumentDbCollectionOptions> collectionOptions,
            IResourcesRegionalDocumentDbClientProvider clientProvider,
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
            options.PartitioningStrategy = PartitioningStrategy.IdOnly;
        }
    }
}
