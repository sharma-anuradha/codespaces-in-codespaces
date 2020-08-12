// <copyright file="StartupFrontEnd.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.AspNetCore.Hosting;
using Microsoft.VsSaaS.Azure.Cosmos;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.IdentityMap;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.AzureCosmosDb;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.AzureClient;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <inheritdoc/>
    public class StartupFrontEnd : CommonStartupBase<AppSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartupFrontEnd"/> class.
        /// </summary>
        /// <param name="hostingEnvironment">The hosting environment for the server.</param>
        public StartupFrontEnd(IWebHostEnvironment hostingEnvironment)
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

            var frontEndAppSettings = appSettings.FrontEnd;
            services.AddSingleton(frontEndAppSettings);
            services.AddSingleton<ISkuUtils, SkuUtils>();

            // Add the environment manager and the cloud environment repository.
            services.AddEnvironmentManager(
                frontEndAppSettings.EnvironmentManagerSettings,
                frontEndAppSettings.EnvironmentMonitorSettings,
                false,
                true);

            // Add the plan manager and the plan management repository
            services.AddPlanManager(frontEndAppSettings.PlanManagerSettings, false);

            // Add the billing event manager and the billing event repository
            services.AddBillingEventManager (
                frontEndAppSettings.BillingSettings,
                frontEndAppSettings.BillingMeterSettings,
                frontEndAppSettings.UseMocksForLocalDevelopment);

            // Add the subscription manager
            services.AddSubscriptionManagers(
                frontEndAppSettings.SubscriptionManagerSettings,
                frontEndAppSettings.UseMocksForLocalDevelopment);
            services.AddSubscriptionsHttpProvider(
                 options =>
                 {
                     options.BaseAddress = ValidationUtil.IsRequired(frontEndAppSettings.RPaaSSettings.RegisteredSubscriptionsUrl, nameof(frontEndAppSettings.RPaaSSettings.RegisteredSubscriptionsUrl));
                 });

            // Add FirstPartyAppSettings
            services.AddSingleton(appSettings.FirstPartyAppSettings);

            // Add the secret store manager
            services.AddSecretStoreManager();

            // Add the Live Share user profile and workspace providers.
            services
                .AddUserProfile(
                    options =>
                    {
                        options.BaseAddress = ValidationUtil.IsRequired(frontEndAppSettings.VSLiveShareApiEndpoint, nameof(frontEndAppSettings.VSLiveShareApiEndpoint));
                    })
                .AddWorkspaceProvider(
                    options =>
                    {
                        options.BaseAddress = ValidationUtil.IsRequired(frontEndAppSettings.VSLiveShareApiEndpoint, nameof(frontEndAppSettings.VSLiveShareApiEndpoint));
                    },
                    appSettings.FrontEnd.UseMocksForLocalDevelopment && !appSettings.FrontEnd.UseFakesForCECLIDevelopmentWithLocalDocker);

            // Add the back-end http client and specific http rest clients.
            services.AddBackEndHttpClient(
                options =>
                {
                    options.BaseAddress = ValidationUtil.IsRequired(frontEndAppSettings.BackEndWebApiBaseAddress, nameof(frontEndAppSettings.BackEndWebApiBaseAddress));
                },
                frontEndAppSettings.UseMocksForLocalDevelopment && !frontEndAppSettings.UseBackEndForLocalDevelopment,
                frontEndAppSettings.UseFakesForCECLIDevelopmentWithLocalDocker && !frontEndAppSettings.UseBackEndForLocalDevelopment,
                frontEndAppSettings.UseFakesLocalDockerImage,
                frontEndAppSettings.UseFakesPublishedCLIPath);

            // Configure mappings betwen REST API models and internal models.
            services.AddModelMapper();

            // Add the certificate settings.
            services.AddSingleton(appSettings.AuthenticationSettings);

            // Add ClaimedDistributedLease
            services.AddSingleton<IClaimedDistributedLease, ClaimedDistributedLease>();

            // VS SaaS services with first party app JWT authentication.
            services.AddVsSaaSHostingWithJwtBearerAuthentication2 (
                HostingEnvironment,
                new LoggingBaseValues(),
                JwtBearerUtility.ConfigureAadOptions,
                keyVaultSecretOptions =>
                {
                    var servicePrincipal = ApplicationServicesProvider.GetRequiredService<IServicePrincipal>();
                    keyVaultSecretOptions.ServicePrincipalClientId = servicePrincipal.ClientId;
                    keyVaultSecretOptions.GetServicePrincipalClientSecretAsyncCallback = servicePrincipal.GetClientSecretAsync;
                },
                null,
                true,
                defaultAuthenticationScheme: null, // This auth scheme will run for all requests regardless of if the Controller specifies it
                jwtBearerAuthenticationScheme: JwtBearerUtility.AadAuthenticationScheme) // This auth scheme gets configured with JwtBearerUtility.ConfigureAadOptions above
                .AddValidatedPrincipalIdentityHandler() // handle validated user principal
                .AddIdentityMap();                      // map user IDs for the validated user principal

            // Add user authentication using VSO (Cascade) tokens.
            services.AddAuthentication((options) =>
            {
                options.DefaultForbidScheme = JwtBearerUtility.VsoAuthenticationScheme;
            }).AddVsoJwtBearerAuthentication();

            // Add custom authentication (rpaas, VM tokens) and VM token validator.
            services.AddCustomFrontEndAuthentication(
                HostingEnvironment,
                new RedisCacheOptions
                {
                    // TODO: make this required -- but it isn't configured yet.
                    RedisConnectionString = frontEndAppSettings.RedisConnectionString,
                },
                ValidationUtil.IsRequired(frontEndAppSettings.RPaaSSettings, nameof(frontEndAppSettings.RPaaSSettings)))
                .AddCertificateCredentialCacheFactory();

            services.AddBlobStorageClientProvider<BlobStorageClientProvider>(options =>
            {
                var controlPlaneAzureResourceAccessor = Services.GetRequiredService<IControlPlaneAzureResourceAccessor>();
                var (accountName, accountKey) = controlPlaneAzureResourceAccessor.GetStampStorageAccountAsync().Result;
                options.AccountName = accountName;
                options.AccountKey = accountKey;
            });

            services.AddAuthorization();

            // Adding developer personal stamp settings and resource name builder.
            var developerPersonalStampSettings = new DeveloperPersonalStampSettings(AppSettings.DeveloperPersonalStamp && HostingEnvironment.IsDevelopment(), AppSettings.DeveloperAlias, AppSettings.DeveloperKusto);
            services.AddSingleton(developerPersonalStampSettings);
            services.AddSingleton<IResourceNameBuilder, ResourceNameBuilder>();

            // Add front-end/back-end common services -- secrets, service principal, control-plane resources.
            services.AddCapacityManager(develperPersonalStamp: developerPersonalStampSettings.DeveloperStamp, mocksSettings: null);
            ConfigureSecretsProvider(services);
            ConfigureCommonServices(services, AppSettings, AppSettings.DeveloperPersonalStamp && AppSettings.DeveloperKusto, out var loggingBaseValues);

            var databaseId = new ResourceNameBuilder(developerPersonalStampSettings).GetCosmosDocDBName(Requires.NotNull(appSettings.AzureCosmosDbDatabaseId, nameof(appSettings.AzureCosmosDbDatabaseId)));

            // Both DocumentDB and Cosmos DB client providers point to the same instance database.
            services
                .AddDocumentDbClientProvider(options =>
                {
                    var controlPlaneAzureResourceAccessor = Services.GetRequiredService<IControlPlaneAzureResourceAccessor>();
                    var (hostUrl, authKey) = controlPlaneAzureResourceAccessor.GetGlobalCosmosDbAccountAsync().Result;
                    options.HostUrl = hostUrl;
                    options.AuthKey = authKey;
                    options.DatabaseId = databaseId;
                    options.UseMultipleWriteLocations = true;
                    options.PreferredLocation = CurrentAzureLocation.ToString();
                })
                .AddRegionalDocumentDbClientProvider(options =>
                {
                    var controlPlaneAzureResourceAccessor = Services.GetRequiredService<IControlPlaneAzureResourceAccessor>();
                    var (hostUrl, authKey) = controlPlaneAzureResourceAccessor.GetRegionalCosmosDbAccountAsync().Result;
                    options.HostUrl = hostUrl;
                    options.AuthKey = authKey;
                    options.DatabaseId = databaseId;
                    options.UseMultipleWriteLocations = true;
                    options.PreferredLocation = CurrentAzureLocation.ToString();
                })
                .AddCosmosClientProvider(options =>
                {
                    var controlPlaneAzureResourceAccessor = Services.GetRequiredService<IControlPlaneAzureResourceAccessor>();
                    var (hostUrl, authKey) = controlPlaneAzureResourceAccessor.GetGlobalCosmosDbAccountAsync().Result;
                    options.HostUrl = hostUrl;
                    options.AuthKey = authKey;
                    options.DatabaseId = databaseId;
                    options.ApplicationLocation = CurrentAzureLocation;
                });

            // Add HeartBeat data handlers
            services.AddHeartBeatDataHandlers();

            services.AddTokenProvider(appSettings.AuthenticationSettings);

            services.AddVsoDocumentDbCollection<ResourcePoolSettingsRecord, IResourcePoolSettingsRepository, CosmosDbResourcePoolSettingsRepository>(
                CosmosDbResourcePoolSettingsRepository.ConfigureOptions);
            services.AddVsoDocumentDbCollection<ResourcePoolStateSnapshotRecord, IResourcePoolStateSnapshotRepository, CosmosDbResourcePoolStateSnapshotRepository>(
                CosmosDbResourcePoolStateSnapshotRepository.ConfigureOptions);

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

            var isProduction = env.IsProduction();

            // We need to enable localhost:3000 CORS headers on dev for Portal development purposes
            // and the current stamp CORS for all environments
            if (isProduction)
            {
                app.UseCors("ProdCORSPolicy");
            }
            else
            {
                app.UseCors("NonProdCORSPolicy");
            }

            // Frameworks
            app.UseStaticFiles();
            app.UseRouting();

            // Use VS SaaS middleware.
            app.UseVsSaaS(!isProduction);

            // Finish setting up config
            var frontEndAppSettings = app.ApplicationServices.GetService<AppSettings>().FrontEnd;
            var systemConfig = app.ApplicationServices.GetService<ISystemConfiguration>();
            frontEndAppSettings.EnvironmentManagerSettings.Init(systemConfig);
            frontEndAppSettings.PlanManagerSettings.Init(systemConfig);
            frontEndAppSettings.EnvironmentMonitorSettings.Init(systemConfig);

            app.UseEndpoints(x =>
            {
                x.MapControllers();
            });
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
