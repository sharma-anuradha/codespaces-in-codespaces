// <copyright file="RegionalCloudEnvironmentRepositoryFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// A factory used to create <see cref="ICloudEnvironmentRepository"/> instances.
    /// </summary>
    public class RegionalCloudEnvironmentRepositoryFactory : IRegionalCloudEnvironmentRepositoryFactory
    {
        private readonly ConcurrentDictionary<AzureLocation, IRegionalCloudEnvironmentRepository> cachedRepositories = new ConcurrentDictionary<AzureLocation, IRegionalCloudEnvironmentRepository>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionalCloudEnvironmentRepositoryFactory"/> class.
        /// </summary>
        /// <param name="crossRegionControlPlaneInfo">The cross-region control plane info.</param>
        /// <param name="clientProviderFactory">The client provider factory.</param>
        /// <param name="collectionOptions">The collection options.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="environmentManagerSettings">The environment manager settings.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public RegionalCloudEnvironmentRepositoryFactory(
            ICrossRegionControlPlaneInfo crossRegionControlPlaneInfo,
            IRegionalDocumentDbClientProviderFactory clientProviderFactory,
            IOptionsMonitor<DocumentDbCollectionOptions> collectionOptions,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            EnvironmentManagerSettings environmentManagerSettings,
            LogValueSet defaultLogValues)
        {
            CrossRegionControlPlaneInfo = Requires.NotNull(crossRegionControlPlaneInfo, nameof(crossRegionControlPlaneInfo));
            ClientProviderFactory = Requires.NotNull(clientProviderFactory, nameof(clientProviderFactory));
            CollectionOptions = Requires.NotNull(collectionOptions, nameof(collectionOptions));
            HealthProvider = Requires.NotNull(healthProvider, nameof(healthProvider));
            LoggerFactory = Requires.NotNull(loggerFactory, nameof(loggerFactory));
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
            DefaultLogValues = Requires.NotNull(defaultLogValues, nameof(defaultLogValues));
        }

        private ICrossRegionControlPlaneInfo CrossRegionControlPlaneInfo { get; }

        private IRegionalDocumentDbClientProviderFactory ClientProviderFactory { get; }

        private IOptionsMonitor<DocumentDbCollectionOptions> CollectionOptions { get; }

        private IHealthProvider HealthProvider { get; }

        private IDiagnosticsLoggerFactory LoggerFactory { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private LogValueSet DefaultLogValues { get; }

        /// <inheritdoc/>
        public IRegionalCloudEnvironmentRepository GetRegionalRepository(AzureLocation controlPlaneLocation, IDiagnosticsLogger logger)
        {
            // Get the cached collection if one exists
            if (cachedRepositories.TryGetValue(controlPlaneLocation, out var repository))
            {
                return repository;
            }

            if (!CrossRegionControlPlaneInfo.AllControlPlaneInfos.TryGetValue(controlPlaneLocation, out var controlPlaneInfo))
            {
                logger.AddValue("RegionalLocation", controlPlaneLocation.ToString());
                logger.LogError("regional_cloud_environment_repository_factory_get_regional_repository_error");
                return null;
            }

            var clientProvider = ClientProviderFactory.GetRegionalClientProvider(controlPlaneLocation);

            repository = new RegionalCloudEnvironmentRepository(CollectionOptions, clientProvider, controlPlaneInfo, HealthProvider, LoggerFactory, EnvironmentManagerSettings, DefaultLogValues);
            repository = cachedRepositories.GetOrAdd(controlPlaneLocation, repository);

            return repository;
        }
    }
}
