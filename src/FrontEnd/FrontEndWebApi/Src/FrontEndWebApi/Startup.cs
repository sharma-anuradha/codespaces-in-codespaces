// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi
{
    /// <summary>
    /// Configures the ASP.NET Core pipeline for HTTP requests.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="hostingEnvironment">The hosting environment for the server.</param>
        public Startup(IHostingEnvironment hostingEnvironment)
        {
            HostingEnvironment = hostingEnvironment;

            var builder = new ConfigurationBuilder()
                .SetBasePath(hostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.secrets.json", optional: true)
                .AddJsonFile($"appsettings.{hostingEnvironment.EnvironmentName}.json", optional: true);

            if (!IsRunningInAzure())
            {
                builder = builder.AddJsonFile("appsettings.Local.json", optional: true);
            }

            builder = builder.AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        private IConfiguration Configuration { get; }

        private IHostingEnvironment HostingEnvironment { get; }

        /// <summary>
        /// This method gets called by the runtime.
        /// Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The services collection that should be configured.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            /*
            services.AddCors(options =>
            {
                options.AddDefaultPolicy((builder) =>
                {
                    builder.WithOrigins(
                        // Localhost for Portal development
                        "https://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            */

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
            var appSettingsConfiguration = Configuration.GetSection("AppSettings");
            var appSettings = appSettingsConfiguration.Get<AppSettings>();
            services.Configure<AppSettings>(appSettingsConfiguration);

            // Logging
            var loggingBaseValues = new LoggingBaseValues
            {
                ServiceName = ServiceConstants.ServiceName,
                CommitId = appSettings.GitCommit,
            };

            // For development, use the default CosmosDB region. Otherwise, when we are running in Azure,
            // use the same region that we are deployed to.
            string preferredCosmosDbRegion = null;
            if (IsRunningInAzure())
            {
                try
                {
                    preferredCosmosDbRegion = Retry
                        .DoAsync(async (attemptNumber) => await AzureInstanceMetadata.GetCurrentLocationAsync())
                        .GetAwaiter()
                        .GetResult();
                }
                catch (Exception e)
                {
                    var logger = new DefaultLoggerFactory().New(new LogValueSet()
                    {
                        { LoggingConstants.Service, loggingBaseValues.ServiceName },
                        { LoggingConstants.CommitId, loggingBaseValues.CommitId },
                    });
                    logger.AddExceptionInfo(e).LogError("error_querying_azure_region_from_instance_metadata");
                }
            }

            if (IsRunningInAzure() && appSettings.UseMocksForLocalDevelopment)
            {
                throw new InvalidOperationException("Cannot use mocks outside of local development");
            }

            // Add the environment manager and the cloud environment repository.
            services.AddEnvironmentManager(appSettings.UseMocksForLocalDevelopment);

            // Add the account manager and the account management repository
            services.AddAccountManager(appSettings.UseBackEndForLocalDevelopment);

            // Add the Live Share user profile and workspace providers.
            services
                .AddUserProfile(
                    options =>
                    {
                        options.BaseAddress = ValidationUtil.IsRequired(appSettings.VSLiveShareApiEndpoint, nameof(appSettings.VSLiveShareApiEndpoint));
                    })
                .AddWorkspaceProvider(
                    options =>
                    {
                        options.BaseAddress = ValidationUtil.IsRequired(appSettings.VSLiveShareApiEndpoint, nameof(appSettings.VSLiveShareApiEndpoint));
                    },
                    appSettings.UseMocksForLocalDevelopment);

            // Add the back-end http client and specific http rest clients.
            services.AddBackEndHttpClient(
                options =>
                {
                    options.BaseAddress = ValidationUtil.IsRequired(appSettings.BackEndWebApiBaseAddress, nameof(appSettings.BackEndWebApiBaseAddress));
                },
                appSettings.UseMocksForLocalDevelopment && !appSettings.UseBackEndForLocalDevelopment);

            // Configure mappings betwen REST API models and internal models.
            services.AddModelMapper();

            // Custom Authentication
            // TODO why isn't this standard VS SaaS SDK Auth?
            services.AddVsSaaSAuthentication(
                HostingEnvironment,
                new RedisCacheOptions
                {
                    // TODO: make this required -- but it isn't configured yet.
                    RedisConnectionString = appSettings.RedisConnectionString,
                },
                new JwtBearerOptions
                {
                    Audiences = ValidationUtil.IsRequired(appSettings.AuthJwtAudiences, nameof(appSettings.AuthJwtAudiences)),
                    Authority = ValidationUtil.IsRequired(appSettings.AuthJwtAuthority, nameof(appSettings.AuthJwtAuthority)),
                });

            // VS SaaS services || BUT NOT VS SaaS authentication
            services.AddVsSaaSHosting(
               HostingEnvironment,
               loggingBaseValues);

            services.AddDocumentDbClientProvider(options =>
            {
                options.HostUrl = appSettings.AzureCosmosDbHost;
                options.AuthKey = appSettings.AzureCosmosDbAuthKey;
                options.DatabaseId = appSettings.AzureCosmosDbDatabaseId;
                options.PreferredLocation = preferredCosmosDbRegion;
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
            var isDevelopment = env.IsDevelopment();

            // Use VS SaaS middleware.
            app.UseVsSaaS(isDevelopment);

            // Frameworks
            app.UseMvc();
            app.UseMvc(ConfigureRoutes);

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
        }

        private void ConfigureRoutes(IRouteBuilder routeBuilder)
        {
            ConfigureRoutesForAccountManagementController(routeBuilder);
        }

        private void ConfigureRoutesForAccountManagementController(IRouteBuilder routeBuilder)
        {
            routeBuilder.MapRoute(
                name: "OnResourceCreationValidate",
                template: "subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceCreationValidate",
                defaults: new { controller = "Account", action = "OnResourceCreationValidate" },
                constraints: new { httpMethod = new HttpMethodRouteConstraint(new[] { "POST" }) });

            routeBuilder.MapRoute(
                name: "OnResourceCreationBegin",
                template: "subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}",
                defaults: new { controller = "Account", action = "OnResourceCreationBegin" },
                constraints: new { httpMethod = new HttpMethodRouteConstraint(new[] { "PUT" }) });

            routeBuilder.MapRoute(
                name: "OnResourceCreationCompleted",
                template: "subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceCreationCompleted",
                defaults: new { controller = "Account", action = "OnResourceCreationCompleted" },
                constraints: new { httpMethod = new HttpMethodRouteConstraint(new[] { "POST" }) });

            routeBuilder.MapRoute(
               name: "OnResourceReadValidate",
               template: "subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceReadValidate",
               defaults: new { controller = "Account", action = "OnResourceReadValidate" },
               constraints: new { httpMethod = new HttpMethodRouteConstraint(new[] { "GET" }) });

            routeBuilder.MapRoute(
                name: "OnResourceListGet",
                template: "subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}",
                defaults: new { controller = "Account", action = "OnResourceListGet" },
                constraints: new { httpMethod = new HttpMethodRouteConstraint(new[] { "GET" }) });

            routeBuilder.MapRoute(
                name: "OnResourceListGetBySubscription",
                template: "subscriptions/{subscriptionId}/providers/{providerNamespace}/{resourceType}",
                defaults: new { controller = "Account", action = "OnResourceListGetBySubscription" },
                constraints: new { httpMethod = new HttpMethodRouteConstraint(new[] { "GET" }) });

            routeBuilder.MapRoute(
                name: "OnResourceDeletionValidate",
                template: "subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceDeletionValidate",
                defaults: new { controller = "Account", action = "OnResourceDeletionValidate" },
                constraints: new { httpMethod = new HttpMethodRouteConstraint(new[] { "POST" }) });
        }
    
    private static bool IsRunningInAzure()
        {
            return Environment.GetEnvironmentVariable(ServiceConstants.RunningInAzureEnvironmentVariable) == ServiceConstants.RunningInAzureEnvironmentVariableValue;
        }
    }
}
