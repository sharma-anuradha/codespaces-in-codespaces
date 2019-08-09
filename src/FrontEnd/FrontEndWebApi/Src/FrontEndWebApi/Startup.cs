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
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;

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

            // CosmosDB Repositories
            services.AddDocumentDbClientProvider(options =>
            {
                options.HostUrl = appSettings.AzureCosmosDbHost;
                options.AuthKey = appSettings.AzureCosmosDbAuthKey;
                options.DatabaseId = appSettings.AzureCosmosDbDatabaseId;
                options.PreferredLocation = preferredCosmosDbRegion;
            });

            if (IsRunningInAzure() && appSettings.UseMocksForLocalDevelopment)
            {
                throw new InvalidOperationException("Cannot use mocks outside of local development");
            }

            // Add the environment manager and the cloud cloud environment repository.
            services.AddEnvironmentManager(
                sessionSettings =>
                {
                    sessionSettings.DefaultHost = ValidationUtil.IsRequired(appSettings.SessionCallbackDefaultHost, nameof(appSettings.SessionCallbackDefaultHost));
                    sessionSettings.DefaultPath = ValidationUtil.IsRequired(appSettings.SessionCallbackDefaultPath, nameof(appSettings.SessionCallbackDefaultPath));
                    sessionSettings.PreferredSchema = ValidationUtil.IsRequired(appSettings.SessionCallbackPreferredSchema, nameof(appSettings.SessionCallbackDefaultHost));
                },
                appSettings.UseMocksForLocalDevelopment);

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
                    });

            // Add the back-end http client and specific http rest clients.
            services.AddBackEndHttpClient(
                options =>
                {
                    options.BaseAddress = ValidationUtil.IsRequired(appSettings.BackEndWebApiBaseAddress, nameof(appSettings.BackEndWebApiBaseAddress));
                },
                appSettings.UseMocksForLocalDevelopment);

            // Configure mappings betwen REST API models and internal models.
            services.AddModelMapper();

            // VS SaaS services and VS SaaS authentication
            services.AddVsSaaSHostingWithJwtBearerAuthentication(
               HostingEnvironment,
               loggingBaseValues,
               authConfigOptions =>
               {
                   authConfigOptions.AudienceAppIds = ValidationUtil.IsRequired(appSettings.AuthJwtAudiences, nameof(AppSettings.AuthJwtAudiences))?.Split(',');
                   authConfigOptions.Authority = ValidationUtil.IsRequired(appSettings.AuthJwtAuthority, nameof(appSettings.AuthJwtAuthority));
                   authConfigOptions.IsEmailClaimRequired = false;
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

        private static bool IsRunningInAzure()
        {
            return Environment.GetEnvironmentVariable(ServiceConstants.RunningInAzureEnvironmentVariable) == ServiceConstants.RunningInAzureEnvironmentVariableValue;
        }
    }
}
