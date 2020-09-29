// <copyright file="GlobalSystemConfigurationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Backend.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.AzureCosmosDb;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common
{
    /// <summary>
    /// A document db collection of <see cref="SystemConfigurationRecord"/>.
    /// </summary>
    [DocumentDbCollectionId(CollectionName)]
    public class GlobalSystemConfigurationRepository : CachedCosmosDbSystemConfigurationRepositoryV2, IGlobalSystemConfigurationRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalSystemConfigurationRepository"/> class.
        /// </summary>
        /// <param name="collectionOptions">The colleciton options.</param>
        /// <param name="clientProvider">The document db client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public GlobalSystemConfigurationRepository(
            IOptionsMonitor<DocumentDbCollectionOptions> collectionOptions,
            IResourcesGlobalDocumentDbClientProvider clientProvider,
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
    }
}