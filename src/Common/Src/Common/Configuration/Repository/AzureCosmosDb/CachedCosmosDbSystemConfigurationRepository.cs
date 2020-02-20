// <copyright file="CachedCosmosDbSystemConfigurationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.AzureCosmosDb
{
    /// <summary>
    /// A document db collection of <see cref="SystemConfigurationRecord"/>.
    /// </summary>
    [DocumentDbCollectionId(CollectionName)]
    public class CachedCosmosDbSystemConfigurationRepository : DocumentDbCollectionCached<SystemConfigurationRecord>, ISystemConfigurationRepository
    {
        /// <summary>
        /// The cosmos db collection name.
        /// </summary>
        public const string CollectionName = "configuration";

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedCosmosDbSystemConfigurationRepository"/> class.
        /// </summary>
        /// <param name="collectionOptions">The colleciton options.</param>
        /// <param name="clientProvider">The document db client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        /// <param name="managedCache">The managed cache instance.</param>
        public CachedCosmosDbSystemConfigurationRepository(
            IOptionsMonitor<DocumentDbCollectionOptions> collectionOptions,
            IDocumentDbClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues,
            IManagedCache managedCache)
            : base(
                  collectionOptions,
                  clientProvider,
                  healthProvider,
                  loggerFactory,
                  defaultLogValues,
                  managedCache)
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
            options.CacheExpiry = TimeSpan.FromMinutes(1);
        }
    }
}
