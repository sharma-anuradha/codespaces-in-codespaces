// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.AspNetCore.Hosting;
using Microsoft.VsSaaS.Azure.Cosmos;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Services;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Providers;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Utility;
using Microsoft.VsSaaS.Services.CloudEnvironments.IdentityMap;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring;
using Microsoft.VsSaaS.Services.CloudEnvironments.PcfAgent;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi
{
    /// <summary>
    /// Configures the ASP.NET Core pipeline for HTTP requests.
    /// </summary>
    public class Startup : CommonStartupBase<AppSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="hostingEnvironment">The hosting environment for the server.</param>
        public Startup(IWebHostEnvironment hostingEnvironment)
            : base(hostingEnvironment, ServiceConstants.ServiceName)
        {
        }

        /// <summary>
        /// This method gets called by the runtime.
        /// Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The services collection that should be configured.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Frameworks
            services
                .AddControllers(options =>
                {
                    options.ModelMetadataDetailsProviders.Add(new ExcludeBindingMetadataProvider(typeof(IDiagnosticsLogger)));
                })
                .AddNewtonsoftJson(x =>
                {
                    x.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
                    x.AllowInputFormatterExceptionMessages = HostingEnvironment.IsDevelopment();
                });

            // Configuration
            var logger = new DefaultLoggerFactory().New();
            var appSettings = ConfigureAppSettings(services);
            var frontEndAppSettings = appSettings.FrontEnd;
            var mdmMetricSettings = frontEndAppSettings.MdmMetricSettings;
            var mdmMetricClient = new MdmMetricClient(mdmMetricSettings.GenevaSvc, mdmMetricSettings.Port, IsRunningInAzure(), logger);

            // Enabling metrics publication, only if its turned on at service level.
            if (mdmMetricSettings.Enable)
            {
                mdmMetricClient.StartPublishingMetrics();
            }

            // To handle the exceptions.
            RegisterUnhandledExceptionHandler(mdmMetricClient, logger);

            // Adding it as singleton so that we can access this in the attribute filter for emitting metrics.
            services.AddSingleton(mdmMetricClient);
            services.AddSingleton(mdmMetricSettings);
            services.AddSingleton(frontEndAppSettings);
            services.AddSingleton<ISkuUtils, SkuUtils>();

            if (HostingEnvironment.IsDevelopment())
            {
                // Enable PII data in logs for Dev
                IdentityModelEventSource.ShowPII = true;
            }

            if (!IsRunningInAzure() && HostingEnvironment.IsDevelopment())
            {
                services.AddNgrok();
            }

            if (IsRunningInAzure() &&
                (frontEndAppSettings.UseMocksForLocalDevelopment ||
                 frontEndAppSettings.DisableBackgroundTasksForLocalDevelopment ||
                 frontEndAppSettings.UseFakesForCECLIDevelopmentWithLocalDocker))
            {
                throw new InvalidOperationException("Cannot use mocks, fakes, or disable background tasks outside of local development.");
            }

            // Inject a custom image info provider that delegates calls to the backend.
            // IMPORTANT: This MUST be placed before the ConfigureCommonServices call below so
            // that this implementation wins instead of the default implementation.
            services.AddSingleton<ICurrentImageInfoProvider, DelegatedImageInfoProvider>();

            // Add front-end/back-end common services -- secrets, service principal, control-plane resources.
            ConfigureCommonServices(services, AppSettings.DeveloperPersonalStamp && AppSettings.DeveloperKusto, out var loggingBaseValues);

            services.AddCors(options =>
                {
                    var vssaasHeaders = new string[]
                    {
                        HttpConstants.CorrelationIdHeader,
                        HttpConstants.RequestIdHeader,
                    };

                    var currentOrigins = ControlPlaneAzureResourceAccessor.GetStampOrigins();

                    // GitHub endpoints that should have access to all VSO service instances (including prod)
                    currentOrigins.Add("https://github.com");

                    // local dev
                    currentOrigins.Add("https://github.localhost");
                    currentOrigins.Add("http://github.localhost");
                    currentOrigins.Add("https://garage.github.com");
                    currentOrigins.Add("https://*.review-lab.github.com");

                    // workspaces.github.com
                    currentOrigins.Add("https://*.workspaces.github.com");
                    currentOrigins.Add("https://*.workspaces-ppe.github.com");
                    currentOrigins.Add("https://*.workspaces-dev.github.com");

                    // codespaces.github.com
                    currentOrigins.Add("https://*.codespaces.github.com");
                    currentOrigins.Add("https://*.codespaces-ppe.github.com");
                    currentOrigins.Add("https://*.codespaces-dev.github.com");
                    currentOrigins.Add("https://portal.azure.com");
                    currentOrigins.Add("https://df.onecloud.azure-test.net");

                    // github.dev
                    currentOrigins.Add("https://*.github.dev");
                    currentOrigins.Add("https://*.ppe.github.dev");
                    currentOrigins.Add("https://*.dev.github.dev");
                    currentOrigins.Add("https://*.local.github.dev");

                    options.AddPolicy(
                        "ProdCORSPolicy",
                        builder => builder
                            .WithOrigins(currentOrigins.ToArray())
                            .AllowAnyHeader()
                            .WithExposedHeaders(vssaasHeaders)
                            .AllowAnyMethod()
                            .SetIsOriginAllowedToAllowWildcardSubdomains());

                    var currentOriginsDev = currentOrigins.GetRange(0, currentOrigins.Count);

                    // Port forwarding proxy server.
                    currentOriginsDev.Add("https://localhost:4000");

                    options.AddPolicy(
                        "NonProdCORSPolicy",
                        builder => builder
                            .WithOrigins(
                                currentOriginsDev.ToArray())
                            .WithExposedHeaders(vssaasHeaders)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .SetIsOriginAllowedToAllowWildcardSubdomains());
                });

            // Add the environment manager and the cloud environment repository.
            services.AddEnvironmentManager(
                frontEndAppSettings.EnvironmentManagerSettings,
                frontEndAppSettings.EnvironmentMonitorSettings,
                frontEndAppSettings.UseMocksForLocalDevelopment || frontEndAppSettings.UseFakesForCECLIDevelopmentWithLocalDocker,
                frontEndAppSettings.DisableBackgroundTasksForLocalDevelopment);

            // Add the plan manager and the plan management repository
            services.AddPlanManager(frontEndAppSettings.PlanManagerSettings, frontEndAppSettings.UseMocksForLocalDevelopment);

            // Add the billing event manager and the billing event repository
            services.AddBillingEventManager(frontEndAppSettings.UseMocksForLocalDevelopment);

            // Add the subscription managers
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

            if (!frontEndAppSettings.DisableBackgroundTasksForLocalDevelopment)
            {
                // Add the plan background worker
                services.AddPlanWorker();

                // Add the Billing SubmissionWorker
                services.AddBillingSubmissionWorker(frontEndAppSettings.UseMocksForLocalDevelopment);

                // Add the billing backgroud worker
                services.AddBillingWorker();

                // Add PCF Agent.
                if (frontEndAppSettings.PrivacyCommandFeedSettings?.IsPcfEnabled == true)
                {
                    services.AddPcfAgent(frontEndAppSettings.PrivacyCommandFeedSettings, frontEndAppSettings.UseMocksForLocalDevelopment);
                }

                // Add subscription manager background workers
                services.AddSubscriptionWorkers();
            }

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
            services.AddVsSaaSHostingWithJwtBearerAuthentication2(
                HostingEnvironment,
                loggingBaseValues,
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
            services.AddAuthentication().AddVsoJwtBearerAuthentication();

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
                var (accountName, accountKey) = ControlPlaneAzureResourceAccessor.GetStampStorageAccountAsync().Result;
                options.AccountName = accountName;
                options.AccountKey = accountKey;
            });

            services.AddAuthorization();

            // Adding developer personal stamp settings and resource name builder.
            var developerPersonalStampSettings = new DeveloperPersonalStampSettings(AppSettings.DeveloperPersonalStamp, AppSettings.DeveloperAlias, AppSettings.DeveloperKusto);
            services.AddSingleton(developerPersonalStampSettings);
            services.AddSingleton<IResourceNameBuilder, ResourceNameBuilder>();

            // Set the forwarding hostname for local development.
            services.AddServiceUriBuilder(frontEndAppSettings.ForwardingHostForLocalDevelopment);

            // Both DocumentDB and Cosmos DB client providers point to the same instance database.
            services
                .AddDocumentDbClientProvider(options =>
                {
                    var (hostUrl, authKey) = ControlPlaneAzureResourceAccessor.GetInstanceCosmosDbAccountAsync().Result;
                    options.HostUrl = hostUrl;
                    options.AuthKey = authKey;
                    options.DatabaseId = new ResourceNameBuilder(developerPersonalStampSettings).GetCosmosDocDBName(Requires.NotNull(appSettings.AzureCosmosDbDatabaseId, nameof(appSettings.AzureCosmosDbDatabaseId)));
                    options.UseMultipleWriteLocations = true;
                    options.PreferredLocation = CurrentAzureLocation.ToString();
                })
                .AddCosmosClientProvider(options =>
                {
                    var (hostUrl, authKey) = ControlPlaneAzureResourceAccessor.GetInstanceCosmosDbAccountAsync().Result;
                    options.HostUrl = hostUrl;
                    options.AuthKey = authKey;
                    options.DatabaseId = new ResourceNameBuilder(developerPersonalStampSettings).GetCosmosDocDBName(Requires.NotNull(appSettings.AzureCosmosDbDatabaseId, nameof(appSettings.AzureCosmosDbDatabaseId)));
                    options.ApplicationLocation = CurrentAzureLocation;
                });

            // Add HeartBeat data handlers
            services.AddHeartBeatDataHandlers();

            services.AddTokenProvider(appSettings.AuthenticationSettings);

            // OpenAPI/swagger
            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc(ServiceConstants.CurrentApiVersion, new OpenApiInfo()
                {
                    Title = ServiceConstants.ServiceName,
                    Description = ServiceConstants.ServiceDescription,
                    Version = ServiceConstants.CurrentApiVersion,
                });

                x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT auth token for the user. Example: eyJ2...",
                });
                x.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme()
                        {
                            Reference = new OpenApiReference()
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer",
                            },
                        },
                        new string[0]
                    },
                });

                x.SchemaFilter<EnumSchemaFilter>();
            });
        }

        /// <summary>
        /// // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
            if (!IsRunningInAzure() && HostingEnvironment.IsDevelopment() && AppSettings.GenerateLocalHostNameFromNgrok)
            {
                var ngrokHosted = app.ApplicationServices.GetService<NgrokHostedService>();

                // Normally, the Ngrok service would start up after the application has started to launch.
                // We need it to start sooner than that, since we need the hostname in advance.
                ngrokHosted.OnApplicationStarted().Wait();
                var tunnels = ngrokHosted.GetTunnelsAsync().Result;
                ConfigureLocalHostname(new Uri(tunnels.First().PublicURL).Host);
            }

            var frontEndAppSettings = app.ApplicationServices.GetService<AppSettings>().FrontEnd;
            var systemConfig = app.ApplicationServices.GetService<ISystemConfiguration>();
            frontEndAppSettings.EnvironmentManagerSettings.Init(systemConfig);
            frontEndAppSettings.PlanManagerSettings.Init(systemConfig);
            frontEndAppSettings.EnvironmentMonitorSettings.Init(systemConfig);
            frontEndAppSettings.SubscriptionManagerSettings.Init(systemConfig);

            app.UseEndpoints(x =>
            {
                x.MapControllers();
            });

            // Swagger/OpenAPI
            app.UseSwagger(x =>
            {
                x.RouteTemplate = "api/{documentName}/swagger";
                x.PreSerializeFilters.Add((swaggerDoc, request) =>
                {
                    var scheme = request.Host.Host == "localhost" ? "http" : "https";
                    var host = request.Host.Value;

                    swaggerDoc.Servers.Add(new OpenApiServer()
                    {
                        Url = $"{scheme}://{host}",
                    });
                });
            });

            app.UseSwaggerUI(c =>
            {
                // UI should be visible from /api/swagger endpoint. Note that leading slash
                // is not present on the RoutePrefix below. This is intentional, because it
                // doesn't work correctly if you add it. I think this is a bug in the Swashbuckle
                // Swagger UI implementation.
                c.RoutePrefix = "api/swagger";

                // The swagger json document is served up from /api/v1/swagger.
                c.SwaggerEndpoint($"/api/{ServiceConstants.CurrentApiVersion}/swagger", ServiceConstants.EndpointName);
                c.DisplayRequestDuration();
            });

            Warmup(app);
        }

        private static void RegisterUnhandledExceptionHandler(IMdmMetricClient mdmMetricClient, IDiagnosticsLogger logger)
        {
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                // Stop metric publication gracefully and emit the remaining metrics via MDM.
                mdmMetricClient.StopPublishingMetrics();

                // In future we may take a heap dump of it here.
                logger.LogCritical($"Process terminating: {e.IsTerminating}\n {e.ExceptionObject.ToString()}");
            };
        }

        private class EnumSchemaFilter : ISchemaFilter
        {
            public void Apply(OpenApiSchema model, SchemaFilterContext context)
            {
                if (context.Type.IsEnum)
                {
                    model.Enum.Clear();
                    foreach (var value in Enum.GetValues(context.Type))
                    {
                        var displayString = $"{(int)value} ({value.ToString()})";
                        model.Enum.Add(new OpenApiString(displayString));
                    }
                }
            }
        }
    }
}
