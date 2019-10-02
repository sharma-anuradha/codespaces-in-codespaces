// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveshareAuthentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;

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
        public Startup(IHostingEnvironment hostingEnvironment)
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
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(x =>
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
                    var currentDomain = $"https://{ControlPlaneAzureResourceAccessor.GetDNSHostName()}";
                    options.AddPolicy("ProdCORSPolicy",
                        builder => builder
                            .WithOrigins(currentDomain)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                        );

                    options.AddPolicy("NonProdCORSPolicy",
                        builder => builder
                            .WithOrigins(
                                "https://localhost:3000",
                                currentDomain
                            )
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                        );
                }
            );

            // Add the account manager and the account management repository
            services.AddAccountManager(frontEndAppSettings.UseMocksForLocalDevelopment);

            // Add the billing event manager and the billing event repository
            services.AddBillingEventManager(frontEndAppSettings.UseMocksForLocalDevelopment);

            // Add the environment manager and the cloud environment repository.
            services.AddEnvironmentManager(frontEndAppSettings.EnvironmentManagerSettings, frontEndAppSettings.UseMocksForLocalDevelopment || frontEndAppSettings.UseFakesForCECLIDevelopmentWithLocalDocker);

            // Add the billing backgroud worker
            services.AddBillingWorker(frontEndAppSettings.UseMocksForLocalDevelopment);

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

            // VM Token validator
            services.AddVMTokenValidator();

            // Custom Authentication
            // TODO why isn't this standard VS SaaS SDK Auth?
            services.AddVsSaaSAuthentication(
                HostingEnvironment,
                new RedisCacheOptions
                {
                    // TODO: make this required -- but it isn't configured yet.
                    RedisConnectionString = frontEndAppSettings.RedisConnectionString,
                },
                new JwtBearerOptions
                {
                    Audiences = ValidationUtil.IsRequired(frontEndAppSettings.AuthJwtAudiences, nameof(frontEndAppSettings.AuthJwtAudiences)),
                    Authority = ValidationUtil.IsRequired(frontEndAppSettings.AuthJwtAuthority, nameof(frontEndAppSettings.AuthJwtAuthority)),
                },
                ValidationUtil.IsRequired(frontEndAppSettings.RPSaaSAuthorityString, nameof(frontEndAppSettings.RPSaaSAuthorityString)));

            // VS SaaS services || BUT NOT VS SaaS authentication
            services.AddVsSaaSHosting(HostingEnvironment, loggingBaseValues);
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
                options.PreferredLocation = CurrentAzureLocation.ToString();
            });

            // OpenAPI/swagger
            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc(ServiceConstants.CurrentApiVersion, new Info()
                {
                    Title = ServiceConstants.ServiceName,
                    Description = ServiceConstants.ServiceDescription,
                    Version = ServiceConstants.CurrentApiVersion,
                });

                x.DescribeAllEnumsAsStrings();

                x.AddSecurityDefinition("Bearer", new ApiKeyScheme()
                {
                    Type = "apiKey",
                    Description = "Paste JWT token with Bearer in front. Example: Bearer eyJ2...",
                    Name = "Authorization",
                    In = "header",
                });

                x.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>()
                {
                    { "Bearer", new string[0] },
                });
            });
        }

        /// <summary>
        /// // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder used to set up the pipeline.</param>
        /// <param name="env">The hosting environment for the server.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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

            // Use VS SaaS middleware.
            app.UseVsSaaS(!isProduction);

            // Frameworks
            app.UseMvc();

            // Swagger/OpenAPI
            app.UseSwagger(x =>
            {
                x.RouteTemplate = "api/{documentName}/swagger";
                x.PreSerializeFilters.Add((swaggerDoc, request) =>
                {
                    swaggerDoc.Host = request.Host.Value;
                    swaggerDoc.Schemes = new[] { request.Host.Host == "localhost" ? "http" : "https" };
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
