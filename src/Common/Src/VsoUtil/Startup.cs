// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.AzureCosmosDb;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine;
using Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.AzureClient;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// Configures the ASP.NET Core pipeline for HTTP requests.
    /// </summary>
    public partial class Startup : CommonStartupBase<AppSettingsBase>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="hostingEnvironment">The hosting environment for the server.</param>
        public Startup(IWebHostEnvironment hostingEnvironment)
            : base(hostingEnvironment, "VsoUtil")
        {
        }

        /// <summary>
        /// Gets or Sets the Service Provider object.
        /// </summary>
        public static IServiceProvider Services { get; set; }

        /// <summary>
        /// This method gets called by the runtime.
        /// Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The services collection that should be configured.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Configuration AppSettings
            var appSettings = ConfigureAppSettings(services);

            if (string.IsNullOrEmpty(appSettings.ControlPlaneSettings.SubscriptionId))
            {
                appSettings.ControlPlaneSettings.SubscriptionId =
                    HostingEnvironment.IsDevelopment() ? "86642df6-843e-4610-a956-fdd497102261" :
                    HostingEnvironment.IsStaging() ? "a3c9bfe3-6696-4b72-a51a-48d3f6e69a24" :
                    HostingEnvironment.IsProduction() ? "979523fb-a19c-4bb0-a8ee-cef29597b0a4" :
                    null;
            }

            // Adding developer personal stamp settings and resource name builder.
            var developerPersonalStampSettings = new DeveloperPersonalStampSettings(AppSettings.DeveloperPersonalStamp && HostingEnvironment.IsDevelopment(), AppSettings.DeveloperAlias, false);
            services.AddSingleton(developerPersonalStampSettings);
            services.AddSingleton<IResourceNameBuilder, ResourceNameBuilder>();

            // Add front-end/back-end common services -- secrets, service principal, control-plane resources.
            services.AddCapacityManager(developerPersonalStamp: developerPersonalStampSettings.DeveloperStamp, mocksSettings: null);
            ConfigureSecretsProvider(services);
            ConfigureCommonServices(services, appSettings, false, out var _);

            // Job Queue consumer telemetry
            services.AddJobQueueTelemetrySummary();

            var documentId = new ResourceNameBuilder(developerPersonalStampSettings).GetCosmosDocDBName(appSettings.AzureCosmosDbDatabaseId);

            // Add stamp database access
            services.AddDocumentDbClientProvider(options =>
            {
                var controlPlaneAzureResourceAccessor = Services.GetRequiredService<IControlPlaneAzureResourceAccessor>();
                var (hostUrl, authKey) = controlPlaneAzureResourceAccessor.GetStampCosmosDbAccountAsync().Result;
                options.ConnectionMode = Microsoft.Azure.Documents.Client.ConnectionMode.Direct;
                options.ConnectionProtocol = Microsoft.Azure.Documents.Client.Protocol.Tcp;
                options.HostUrl = hostUrl;
                options.AuthKey = authKey;
                options.DatabaseId = documentId;
                options.PreferredLocation = CurrentAzureLocation.ToString();
                options.UseMultipleWriteLocations = false;
            });

            // Add resources database access
            services.AddResourcesGlobalDocumentDbClientProvider(options =>
             {
                 var controlPlaneAzureResourceAccessor = Services.GetRequiredService<IControlPlaneAzureResourceAccessor>();
                 var (hostUrl, authKey) = controlPlaneAzureResourceAccessor.GetResourcesGlobalCosmosDbAccountAsync().Result;
                 options.HostUrl = hostUrl;
                 options.AuthKey = authKey;
                 options.DatabaseId = documentId;
                 options.UseMultipleWriteLocations = true;
                 options.PreferredLocation = CurrentAzureLocation.ToString();
             });

            services.AddVsoDocumentDbCollection<ResourcePoolSettingsRecord, IResourcePoolSettingsRepository, CosmosDbResourcePoolSettingsRepository>(
                CosmosDbResourcePoolSettingsRepository.ConfigureOptions);
            services.AddVsoDocumentDbCollection<ResourcePoolStateSnapshotRecord, IResourcePoolStateSnapshotRepository, CosmosDbResourcePoolStateSnapshotRepository>(
                CosmosDbResourcePoolStateSnapshotRepository.ConfigureOptions);

            services.AddSingleton<IServiceBusClientProvider, ServiceBusClientProvider>();

            services.AddSingleton<IHealthProvider, NullHealthProvider>();
            services.AddSingleton<IDiagnosticsLoggerFactory, NullDiagnosticsLoggerFactory>();
            services.AddSingleton(new LogValueSet());

            var userToken = GetUserAccessToken();
            if (!string.IsNullOrEmpty(userToken))
            {
                services.Configure<UserTokenAzureClientFactoryOptions>(options =>
                {
                    options.AccessToken = userToken;
                });

                // Overrides the IAzureClientFactory which uses the SP credentials which is set up in ConfigureCommonServices
                services.AddSingleton<IAzureClientFactory, UserTokenAzureClientFactory>();
            }
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder used to set up the pipeline.</param>
        /// <param name="env">The hosting environment for the server.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            ConfigureAppCommon(app);
        }

        /// <inheritdoc/>
        protected override string GetSettingsRelativePath()
        {
            // Use current directory.
            return string.Empty;
        }

        private void ConfigureSecretsProvider(IServiceCollection services)
        {
            // KeyVaultSecretProvider uses logged in identity to get the secrets, with that one could access JIT'ed subscriptions without having to
            // copy secrets out of the keyvault manually. However, it doesn't work for all - by setting "UseSecretFromAppConfig = 1" the default secret
            // from the appconfig will be used - enough for devstamp.
            var appConfigSecret = Environment.GetEnvironmentVariable(CommandBase.UseSecretFromAppConfigEnvVarName);
            if (appConfigSecret != "1")
            {
                services.AddSingleton<ISecretProvider, KeyVaultSecretProvider>();
            }
        }

        private string GetUserAccessToken()
        {
            return Environment.GetEnvironmentVariable(CommandBase.UserAccessTokenEnvVarName);
        }

        private class NullHealthProvider : IHealthProvider
        {
            public bool IsHealthy => true;

            public void MarkUnhealthy(Exception exception, IDiagnosticsLogger logger)
            {
            }
        }
    }
}