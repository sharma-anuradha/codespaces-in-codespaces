// <copyright file="AzureDocumentsProviderService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Async warm bootstrap for the Azure Cosmos provider.
    /// </summary>
    public class AzureDocumentsProviderService : WarmupServiceBase
    {
        private const int MaxFactoryRetries = 10;
        private static readonly TimeSpan FactoryRetryTimeout = TimeSpan.FromSeconds(10);

        private readonly IStartupBase startup;
        private readonly IEnumerable<IAzureDocumentsProviderServiceFactory> azureDocumentsProviderServiceFactories;
        private readonly IOptions<AppSettingsBase> appSettingsProvider;
        private readonly ApplicationServicePrincipal applicationServicePrincipal;
        private readonly ILogger logger;

        public AzureDocumentsProviderService(
            IList<IAsyncWarmup> warmupServices,
            IList<IHealthStatusProvider> healthStatusProviders,
            IStartupBase startup,
            IEnumerable<IAzureDocumentsProviderServiceFactory> azureDocumentsProviderServiceFactories,
            IOptions<AppSettingsBase> appSettingsProvider,
            ApplicationServicePrincipal applicationServicePrincipal,
            ILogger<AzureDocumentsProviderService> logger,
            IDataFormatProvider formatProvider = null)
            : base(warmupServices, healthStatusProviders)
        {
            this.startup = startup;
            this.azureDocumentsProviderServiceFactories = azureDocumentsProviderServiceFactories;
            this.appSettingsProvider = appSettingsProvider;
            this.applicationServicePrincipal = applicationServicePrincipal;
            this.logger = logger;
        }

        private AppSettingsBase AppSettings => this.appSettingsProvider.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool isAzureCosmosConnectionDefined = false;

            if (AppSettings.IsAzureDocumentsProviderEnabled &&
                ((isAzureCosmosConnectionDefined = this.applicationServicePrincipal.IsAzureCosmosConnectionDefined(AppSettings)) ||
                (!string.IsNullOrEmpty(AppSettings.AzureCosmosDbEndpointUrl) &&
                !string.IsNullOrEmpty(AppSettings.AzureCosmosDbAuthKey))))
            {
                try
                {
                    var cosmosConfiguration = isAzureCosmosConnectionDefined ?
                        await this.applicationServicePrincipal.GetAzureCosmosConnection(
                            this.logger,
                            AppSettings,
                            stoppingToken) : (NormalizeSetting(AppSettings.AzureCosmosDbEndpointUrl), NormalizeSetting(AppSettings.AzureCosmosDbAuthKey));

                    var databaseSettings = new DatabaseSettings()
                    {
                        EndpointUrl = cosmosConfiguration.Item1,
                        AuthorizationKey = cosmosConfiguration.Item2,
                        IsProduction = !this.startup.IsDevelopmentEnv,
                    };

                    if (!string.IsNullOrEmpty(this.startup.PreferredLocation) &&
                       DocumentDbLocationPreferenceMap.DefaultPreferences.TryGetValue(this.startup.PreferredLocation, out var locations))
                    {
                        databaseSettings.PreferredRegions = locations.ToArray();
                    }

                    this.logger.LogInformation($"Creating Azure Cosmos provider with Url:'{cosmosConfiguration.Item1}' preferredRegions:{string.Join(',', databaseSettings.PreferredRegions ?? Array.Empty<string>())}");

                    foreach (var factory in this.azureDocumentsProviderServiceFactories)
                    {
                        await CreateAzureDocumentsProviderService(factory, databaseSettings, MaxFactoryRetries, FactoryRetryTimeout, stoppingToken);
                    }
                }
                catch (Exception error)
                {
                    CompleteWarmup(false);
                    this.logger.LogError(error, $"Failed to initialize");
                    throw;
                }
            }
            else
            {
                this.logger.LogWarning($"Azure Cosmos not configured");
            }

            CompleteWarmup(true);
        }

        private static string NormalizeSetting(string s)
        {
            return System.Text.RegularExpressions.Regex.Replace(s, @"\r\n?|\n", string.Empty);
        }

        private async Task CreateAzureDocumentsProviderService(
            IAzureDocumentsProviderServiceFactory azureDocumentsProviderServiceFactory,
            DatabaseSettings databaseSettings,
            int maxRetries,
            TimeSpan retryTime,
            CancellationToken cancellationToken)
        {
            int nextTry = 0;
            var factoryName = azureDocumentsProviderServiceFactory.GetType().Name;

            while (true)
            {
                try
                {
                    await azureDocumentsProviderServiceFactory.CreateAsync(this.startup.ServiceInfo, databaseSettings, cancellationToken);
                    this.logger.LogInformation($"Successfully created Azure Documents provider with factory:'{factoryName}'");
                    break;
                }
                catch (Exception error)
                {
                    this.logger.LogWarning(error, $"Failed to create Azure Cosmos provider with factory:'{factoryName}' retry:{++nextTry}");
                    --maxRetries;
                    if (maxRetries == 0)
                    {
                        throw;
                    }
                    else
                    {
                        await Task.Delay(retryTime, cancellationToken);
                    }
                }
            }
        }
    }
}
