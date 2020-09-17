// <copyright file="RegionalDocumentDbClientProviderFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// A factory used to create <see cref="IRegionalDocumentDbClientProvider"/> instances.
    /// </summary>
    public class RegionalDocumentDbClientProviderFactory : IRegionalDocumentDbClientProviderFactory
    {
        private readonly ConcurrentDictionary<AzureLocation, IRegionalDocumentDbClientProvider> cachedClientProviders = new ConcurrentDictionary<AzureLocation, IRegionalDocumentDbClientProvider>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionalDocumentDbClientProviderFactory"/> class.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="resourceNameBuilder">The resource name builder.</param>
        /// <param name="crossRegionControlPlaneInfo">The cross-region control plane info.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public RegionalDocumentDbClientProviderFactory(
            AppSettingsBase appSettings,
            IResourceNameBuilder resourceNameBuilder,
            ICrossRegionControlPlaneInfo crossRegionControlPlaneInfo,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
        {
            AppSettings = Requires.NotNull(appSettings, nameof(appSettings));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
            CrossRegionControlPlaneInfo = Requires.NotNull(crossRegionControlPlaneInfo, nameof(crossRegionControlPlaneInfo));
            HealthProvider = Requires.NotNull(healthProvider, nameof(healthProvider));
            LoggerFactory = Requires.NotNull(loggerFactory, nameof(loggerFactory));
            DefaultLogValues = Requires.NotNull(defaultLogValues, nameof(defaultLogValues));
        }

        private AppSettingsBase AppSettings { get; }

        private IHealthProvider HealthProvider { get; }

        private IDiagnosticsLoggerFactory LoggerFactory { get; }

        private LogValueSet DefaultLogValues { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private ICrossRegionControlPlaneInfo CrossRegionControlPlaneInfo { get; }

        /// <inheritdoc/>
        public IRegionalDocumentDbClientProvider GetRegionalClientProvider(AzureLocation controlPlaneLocation)
        {
            // Get the cached collection if one exists
            if (cachedClientProviders.TryGetValue(controlPlaneLocation, out var clientProvider))
            {
                return clientProvider;
            }

            if (CrossRegionControlPlaneInfo.AllResourceAccessors.TryGetValue(controlPlaneLocation, out var controlPlaneAzureResourceAccessor))
            {
                var (hostUrl, authKey) = controlPlaneAzureResourceAccessor.GetRegionalCosmosDbAccountAsync().Result;
                var clientOptions = new RegionalDocumentDbClientOptions
                {
                    HostUrl = hostUrl,
                    AuthKey = authKey,
                    DatabaseId = ResourceNameBuilder.GetCosmosDocDBName(AppSettings.AzureCosmosDbDatabaseId),
                    UseMultipleWriteLocations = true,
                    PreferredLocation = controlPlaneLocation.ToString(),
                };

                clientProvider = new RegionalDocumentDbClientProvider(Options.Create(clientOptions), HealthProvider, LoggerFactory, DefaultLogValues);
                clientProvider = cachedClientProviders.GetOrAdd(controlPlaneLocation, clientProvider);
            }

            return clientProvider;
        }
    }
}
