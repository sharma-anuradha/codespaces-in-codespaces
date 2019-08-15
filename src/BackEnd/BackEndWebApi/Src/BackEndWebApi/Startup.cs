// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Settings;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackendWebApi
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
                .AddJsonFile($"appsettings.{hostingEnvironment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

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
            // Frameworks
            services
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(x =>
                {
                    x.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
                    x.AllowInputFormatterExceptionMessages = HostingEnvironment.IsDevelopment();
                });

            // Configuration setup
            var appSettingsConfiguration = Configuration.GetSection("AppSettings");
            var appSettings = appSettingsConfiguration.Get<AppSettings>();
            services.Configure<AppSettings>(appSettingsConfiguration);

            var storageAccountSettingsConfiguration = Configuration.GetSection("StorageAccountSettings");
            var storageAccountSettings = storageAccountSettingsConfiguration.Get<StorageAccountSettings>();
            services.Configure<StorageAccountSettings>(storageAccountSettingsConfiguration);
            services.AddSingleton(storageAccountSettings);

            // Common services
            services.AddSingleton<IDistributedLease, DistributedLease>();

            // Logging setup
            var loggingBaseValues = new LoggingBaseValues
            {
                ServiceName = "BackendWebApi",
                CommitId = appSettings.GitCommit,
            };

            // VsSaaS services
            services.AddVsSaaSHosting(HostingEnvironment, loggingBaseValues);
            services.AddBlobStorageClientProvider<BlobStorageClientProvider>(options =>
            {
                options.AccountKey = storageAccountSettings.StorageAccountKey;
                options.AccountName = storageAccountSettings.StorageAccountName;
            });
            services.AddDocumentDbClientProvider(options =>
            {
                options.ConnectionMode = Microsoft.Azure.Documents.Client.ConnectionMode.Direct;
                options.ConnectionProtocol = Microsoft.Azure.Documents.Client.Protocol.Tcp;
                options.HostUrl = appSettings.AzureCosmosDbHost;
                options.AuthKey = appSettings.AzureCosmosDbKey;
                options.DatabaseId = appSettings.AzureCosmosDbId;
                options.PreferredLocation = appSettings.AzureCosmosPreferredLocation;
                options.UseMultipleWriteLocations = false;
            });

            // Mappers services
            var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddResourceBroker();
                });
            var mapper = config.CreateMapper();
            services.AddSingleton(mapper);

            // System Catalog
            var azureSubscriptionCatalogSettings = Configuration.GetSection("AzureSubscriptionCatalogSettings").Get<AzureSubscriptionCatalogSettings>();
            var skuCatalogSettings = Configuration.GetSection("SkuCatalogSettings").Get<SkuCatalogSettings>();

            services.AddSystemCatalog(
                azureSubscriptionCatalogSettings,
                skuCatalogSettings);

            // Resource Broker
            var resourceBrokerSettingsConfiguration = Configuration.GetSection("ResourceBrokerSettings");
            var resourceBrokerSettings = resourceBrokerSettingsConfiguration.Get<ResourceBrokerSettings>();
            services.Configure<ResourceBrokerSettings>(resourceBrokerSettingsConfiguration);
            services.AddSingleton(resourceBrokerSettings);

            services.AddResourceBroker(
                storageAccountSettings,
                resourceBrokerSettings,
                appSettings);

            // Scaling Engine
            services.AddScalingEngine(
                appSettings);

            // Compute Provider
            services.AddComputeVirtualMachineProvider(
                appSettings);

            // Storage Provider
            services.AddStorageFileShareProvider(
                appSettings);

            // OpenAPI/swagger services
            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("v1", new Info()
                {
                    Title = "BackendWebApi",
                    Description = "Backend API for managing resources needed for Cloud Environments. This API is only exposed internally in the cluster.",
                    Version = "v1",
                });

                x.DescribeAllEnumsAsStrings();
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

            // System Components
            app.UseSystemCatalog(env);
            app.UseScalingEngine(env);
            app.UseResourceBroker(env);
            app.UseComputeVirtualMachineProvider(env);
            app.UseStorageFileShareProvider(env);

            // Use VS SaaS middleware.
            app.UseVsSaaS(isDevelopment);

            // Frameworks
            app.UseMvc();

            // Swagger/OpenAPI
            app.UseSwagger(x =>
            {
                x.RouteTemplate = "/{documentName}/swagger";
                x.PreSerializeFilters.Add((swaggerDoc, request) =>
                {
                    swaggerDoc.Host = request.Host.Value;
                    swaggerDoc.Schemes = new[] { request.Host.Host == "localhost" ? "http" : "https" };
                });
            });

            app.UseSwaggerUI(c =>
            {
                // UI should be visible from /swagger endpoint. Note that leading slash
                // is not present on the RoutePrefix below. This is intentional, because it
                // doesn't work correctly if you add it. I think this is a bug in the Swashbuckle
                // Swagger UI implementation.
                c.RoutePrefix = "swagger";

                // The swagger json document is served up from /v1/swagger.
                c.SwaggerEndpoint("/v1/swagger", "BackendWebApi API v1");
                c.DisplayRequestDuration();
            });
        }
    }
}
