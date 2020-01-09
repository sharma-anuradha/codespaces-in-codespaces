// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.VsSaaS.AspNetCore.Hosting;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.IdentityMap;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveshareAuthentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserSubscriptions;
using Newtonsoft.Json;

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
            var appSettings = ConfigureAppSettings(services);
            var frontEndAppSettings = appSettings.FrontEnd;

            if (IsRunningInAzure() && frontEndAppSettings.UseMocksForLocalDevelopment)
            {
                throw new InvalidOperationException("Cannot use mocks outside of local development");
            }

            // Add front-end/back-end common services -- secrets, service principal, control-plane resources.
            ConfigureCommonServices(services, out var loggingBaseValues);

            services.AddCors(options =>
                {
                    var vssaasHeaders = new string[]
                    {
                        HttpConstants.CorrelationIdHeader,
                        HttpConstants.RequestIdHeader,
                    };

                    var currentOrigins = ControlPlaneAzureResourceAccessor.GetStampOrigins();
                    options.AddPolicy(
                        "ProdCORSPolicy",
                        builder => builder
                            .WithOrigins(currentOrigins.ToArray())
                            .AllowAnyHeader()
                            .WithExposedHeaders(vssaasHeaders)
                            .AllowAnyMethod());

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
                            .AllowAnyMethod());
                });

            // Add the account manager and the account management repository
            services.AddPlanManager(frontEndAppSettings.PlanManagerSettings, frontEndAppSettings.UseMocksForLocalDevelopment);

            // Add the plan background worker
            services.AddPlanWorker();

            // Add the billing event manager and the billing event repository
            services.AddBillingEventManager(frontEndAppSettings.UseMocksForLocalDevelopment);

            // Add the Billing SubmissionWorker
            services.AddBillingSubmissionWorker(frontEndAppSettings.UseMocksForLocalDevelopment);

            // Add the environment manager and the cloud environment repository.
            services.AddEnvironmentManager(frontEndAppSettings.EnvironmentManagerSettings, frontEndAppSettings.UseMocksForLocalDevelopment || frontEndAppSettings.UseFakesForCECLIDevelopmentWithLocalDocker);

            // Add the billing backgroud worker
            services.AddBillingWorker();

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
                    appSettings.FrontEnd.UseMocksForLocalDevelopment && !appSettings.FrontEnd.UseFakesForCECLIDevelopmentWithLocalDocker)
                .AddLiveshareAuthProvider(
                    options =>
                    {
                        options.BaseAddress = ValidationUtil.IsRequired(frontEndAppSettings.VSLiveShareApiEndpoint, nameof(frontEndAppSettings.VSLiveShareApiEndpoint));
                    });

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
            services.AddSingleton(appSettings.CertificateSettings);

            // Add ClaimedDistributedLease
            services.AddSingleton<IClaimedDistributedLease, ClaimedDistributedLease>();

            // VS SaaS services with first party app JWT authentication.
            services.AddVsSaaSHostingWithJwtBearerAuthentication2(
                HostingEnvironment,
                loggingBaseValues,
                JwtBearerUtility.ConfigureOptions,
                keyVaultSecretOptions =>
                {
                    var servicePrincipal = ApplicationServicesProvider.GetRequiredService<IServicePrincipal>();
                    keyVaultSecretOptions.ServicePrincipalClientId = servicePrincipal.ClientId;
                    keyVaultSecretOptions.GetServicePrincipalClientSecretAsyncCallback = servicePrincipal.GetServicePrincipalClientSecretAsync;
                },
                null,
                true,
                JwtBearerUtility.AuthenticationScheme,
                JwtBearerUtility.AuthenticationScheme)
                .AddValidatedPrincipalIdentityHandler() // handle validated user principal
                .AddIdentityMap();                      // map user IDs for the validated user principal

            // Add custom authentication (rpsaas, VM tokens) and VM token validator.
            services.AddCustomFrontEndAuthentication(
                HostingEnvironment,
                new RedisCacheOptions
                {
                    // TODO: make this required -- but it isn't configured yet.
                    RedisConnectionString = frontEndAppSettings.RedisConnectionString,
                },
                ValidationUtil.IsRequired(frontEndAppSettings.RPSaaSAuthorityString, nameof(frontEndAppSettings.RPSaaSAuthorityString)))
                .AddVMTokenValidator();

            services.AddBlobStorageClientProvider<BlobStorageClientProvider>(options =>
            {
                var (accountName, accountKey) = ControlPlaneAzureResourceAccessor.GetStampStorageAccountAsync().Result;
                options.AccountName = accountName;
                options.AccountKey = accountKey;
            });

            services.AddAuthorization(options =>
            {
                // Verify RPSaaS appid exists in bearer claims and is valid
                options.AddPolicy("RPSaaSIdentity", policy => policy.RequireClaim(
                    "appid",
                    new[] { ValidationUtil.IsRequired(frontEndAppSettings.RPSaaSAppIdString, nameof(frontEndAppSettings.RPSaaSAppIdString)) }));
            });

            // Adding developer personal stamp settings and resource name builder.
            var developerPersonalStampSettings = new DeveloperPersonalStampSettings(AppSettings.DeveloperPersonalStamp);
            services.AddSingleton(developerPersonalStampSettings);
            services.AddSingleton<IResourceNameBuilder, ResourceNameBuilder>();

            services.AddServiceUriBuilder(frontEndAppSettings.ForwardingHostForLocalDevelopment);

            services.AddDocumentDbClientProvider(options =>
            {
                var (hostUrl, authKey) = ControlPlaneAzureResourceAccessor.GetInstanceCosmosDbAccountAsync().Result;
                options.HostUrl = hostUrl;
                options.AuthKey = authKey;
                options.DatabaseId = new ResourceNameBuilder(developerPersonalStampSettings).GetCosmosDocDBName(Requires.NotNull(appSettings.AzureCosmosDbDatabaseId, nameof(appSettings.AzureCosmosDbDatabaseId)));
                options.UseMultipleWriteLocations = true;
                options.PreferredLocation = CurrentAzureLocation.ToString();
            });

            // Add HeartBeat data handlers
            services.AddHeartBeatDataHandlers();

            // Add user-subscriptions
            services.AddDocumentDbCollection<UserSubscription, IUserSubscriptionRepository, UserSubscriptionRepository>(UserSubscriptionRepository.ConfigureOptions);

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
            var frontEndAppSettings = app.ApplicationServices.GetService<AppSettings>().FrontEnd;
            var systemConfig = app.ApplicationServices.GetService<ISystemConfiguration>();
            frontEndAppSettings.EnvironmentManagerSettings.Init(systemConfig);
            frontEndAppSettings.PlanManagerSettings.Init(systemConfig);

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
    }
}
