// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.VsSaaS.AspNetCore.Hosting;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Azure.Management;
using Microsoft.VsSaaS.Azure.Metrics;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.DiagnosticsServer.Startup;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApi.Support;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Services;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Warmup;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackendWebApi
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
            : base(hostingEnvironment, "BackEndWebApi")
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
                .AddControllers(o =>
                {
                    o.ModelMetadataDetailsProviders.Add(new RequiredBindingMetadataProvider());
                })
                .AddNewtonsoftJson(x =>
                {
                    x.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
                    x.AllowInputFormatterExceptionMessages = HostingEnvironment.IsDevelopment();
                });

            // Configuration AppSettings
            var appSettings = ConfigureAppSettings(services);

            // To handle the exceptions.
            RegisterUnhandledExceptionHandler(new DefaultLoggerFactory().New());

            if (IsRunningInAzure() && AppSettings.DeveloperPersonalStamp)
            {
                throw new InvalidOperationException("Cannot use DeveloperPersonalStamp outside of local development");
            }

            if (IsRunningInAzure())
            {
                var mocksSettings = AppSettings.BackEnd.MocksSettings;
                if (mocksSettings != null)
                {
                    if (mocksSettings.UseMocksForExternalDependencies ||
                        mocksSettings.UseMocksForResourceBroker ||
                        mocksSettings.UseMocksForResourceProviders)
                    {
                        throw new InvalidOperationException("Cannot use mocks outside of local development");
                    }
                }
            }

            if (!IsRunningInAzure() && HostingEnvironment.IsDevelopment())
            {
                services.AddNgrok();
            }

            // Add front-end/back-end common services -- secrets, service principal, control-plane resources.
            ConfigureCommonServices(services, AppSettings, null, out var loggingBaseValues);

            // Common services
            services.AddSingleton<IDistributedLease, DistributedLease>();
            services.AddSingleton<IClaimedDistributedLease, ClaimedDistributedLease>();

            // VsSaaS services
            services.AddVsSaaSHosting(HostingEnvironment, loggingBaseValues);
            services.AddBlobStorageClientProvider<BlobStorageClientProvider>(options =>
            {
                var (accountName, accountKey) = ControlPlaneAzureResourceAccessor.GetStampStorageAccountAsync().Result;
                options.AccountName = accountName;
                options.AccountKey = accountKey;
            });

            // Adding developer personal stamp settings and resource name builder.
            var developerPersonalStampSettings = new DeveloperPersonalStampSettings(AppSettings.DeveloperPersonalStamp, AppSettings.DeveloperAlias, AppSettings.DeveloperKusto);
            services.AddSingleton(developerPersonalStampSettings);
            services.AddSingleton<IResourceNameBuilder, ResourceNameBuilder>();

            // Adding agent settings.
            services.AddSingleton(AppSettings.AgentSettings);

            // Get the database id
            var databaseId = new ResourceNameBuilder(developerPersonalStampSettings).GetCosmosDocDBName(RequiresNotNullOrEmpty(appSettings.AzureCosmosDbDatabaseId, nameof(appSettings.AzureCosmosDbDatabaseId)));

            // This also makes sure that the default DocumentDbClientOptions points to the regional backend database.
            // If you want to use the global database then make use of the ResourcesGlobalDocumentDbClientOptions in your provider (or just use ResourcesGlobalDocumentDbClientProvider in repository class).
            // If you use the DefaultDbClientProvider in your rrepository, it will point to regional backend database. Same would happen with the usage of ResourcesRegionalDocumentDbClientProvider.
            // Eg. code can be seen in ctors of CosmosDbResourceRepository (points to regional db), RegionalSystemConfigurationRepository(points to regional db) and
            // GlobalSystemConfigurationRepository (points to global db) 
            services.AddDocumentDbClientProvider(options =>
            {
                var (hostUrl, authKey) = ControlPlaneAzureResourceAccessor.GetStampCosmosDbAccountAsync().Result;
                options.ConnectionMode = Microsoft.Azure.Documents.Client.ConnectionMode.Direct;
                options.ConnectionProtocol = Microsoft.Azure.Documents.Client.Protocol.Tcp;
                options.HostUrl = hostUrl;
                options.AuthKey = authKey;
                options.DatabaseId = databaseId;
                options.PreferredLocation = CurrentAzureLocation.ToString();
                options.UseMultipleWriteLocations = false;
            })
            .AddResourcesGlobalDocumentDbClientProvider(options =>
            {
                var (hostUrl, authKey) = ControlPlaneAzureResourceAccessor.GetResourcesGlobalCosmosDbAccountAsync().Result;
                options.HostUrl = hostUrl;
                options.AuthKey = authKey;
                options.DatabaseId = databaseId;
                options.UseMultipleWriteLocations = true;
                options.PreferredLocation = CurrentAzureLocation.ToString();
            });

            // Add the regional Document client provider which uses the default DocumentDbClientOptions and points to regional db.
            services.AddResourcesRegionalDocumentDbClientProvider();

            // Mappers services
            var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddResourceBroker();
                    cfg.AddSecretManager();
                    cfg.CreateMap<JValue, Guid>().ConvertUsing(g => Guid.Parse(g.Value.ToString()));
                });
            var mapper = config.CreateMapper();
            services.AddSingleton(mapper);

            // Resource Broker
            services.AddResourceBroker(
                appSettings.BackEnd.ResourceBrokerSettings,
                appSettings.BackEnd.MocksSettings);

            // Scaling Engine
            services.AddScalingEngine();

            // Compute Provider
            services.AddComputeVirtualMachineProvider(appSettings.BackEnd.MocksSettings);

            // Storage Provider
            services.AddStorageFileShareProvider(
                appSettings.BackEnd.StorageProviderSettings,
                appSettings.BackEnd.MocksSettings);

            // Archive Storage Provider
            services.AddAzureMetrics();
            services.AddAzureManagement();
            services.AddArchiveStorageProvider(appSettings.BackEnd.MocksSettings);

            // Export Storage Provider
            services.AddExportStorageProvider(appSettings.BackEnd.MocksSettings);

            // KeyVault Provider
            services.AddKeyVaultProvider(appSettings.BackEnd.MocksSettings);

            // Disk Provider
            services.AddDiskProvider(appSettings.BackEnd.MocksSettings);

            // Network Interface Provider
            services.AddNetworkInterfaceProvider(appSettings.BackEnd.MocksSettings);

            // Managed Identity Provider
            services.AddManagedIdentityProvider();

            // Capacity Manager
            services.AddCapacityManager(appSettings.DeveloperPersonalStamp, appSettings.BackEnd.MocksSettings);

            // Job Queue consumer telemetry
            services.AddJobQueueTelemetrySummary();

            // Add the certificate settings.
            services.AddSingleton(appSettings.AuthenticationSettings);

            // Add FirstPartyAppSettings
            services.AddSingleton(appSettings.FirstPartyAppSettings);

            // Auth/Token Providers
            services
                .AddKeyVaultSecretReader(keyVaultSecretOptions =>
                {
                    var servicePrincipal = ApplicationServicesProvider.GetRequiredService<IServicePrincipal>();
                    keyVaultSecretOptions.ServicePrincipalClientId = servicePrincipal.ClientId;
                    keyVaultSecretOptions.GetServicePrincipalClientSecretAsyncCallback = servicePrincipal.GetClientSecretAsync;
                })
                .AddTokenProvider(appSettings.AuthenticationSettings);

            // Setup configuration
            services.AddVsoDocumentDbCollection<SystemConfigurationRecord, IRegionalSystemConfigurationRepository, RegionalSystemConfigurationRepository>(
                RegionalSystemConfigurationRepository.ConfigureOptions);

            services.AddVsoDocumentDbCollection<SystemConfigurationRecord, IGlobalSystemConfigurationRepository, GlobalSystemConfigurationRepository>(
                GlobalSystemConfigurationRepository.ConfigureOptions);

            services.AddSingleton<ISystemConfigurationRepository, SystemConfigurationRepository>();
            services.AddSingleton<ICachedSystemConfigurationRepository, SystemConfigurationRepository>();

            // Add the cache system configuration warmup task
            services.AddCacheSystemConfigurationWarmupTask();

            // OpenAPI/swagger
            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("v1", new OpenApiInfo()
                {
                    Title = ServiceName,
                    Description = "Backend API for managing resources needed for Cloud Environments. This API is only exposed internally in the cluster.",
                    Version = "v1",
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

            var isDevelopment = env.IsDevelopment();

            // Emit the startup settings to logs for diagnostics.
            var logger = app.ApplicationServices.GetRequiredService<IDiagnosticsLogger>();

            // Frameworks
            app.UseStaticFiles();
            app.UseRouting();

            // Use VS SaaS middleware. Backend requests are not authenticated since all auth/permissions checks happen in the frontend.
            app.UseVsSaaS(isDevelopment, useAuthentication: false);

            app.UseEndpoints(x =>
            {
                x.MapControllers();
            });

            if (AppSettings.StartDiagnosticsServer)
            {
                Task.Run(async () =>
                {
                    var diag = new DiagnosticsHostedService();
                    if (!await diag.IsDiagnosticsRunningAsync(59330))
                    {
                        // While IsDiagnosticsRunningAsync is run as part of StartAsync, because there are two services starting at once
                        // There could be a race condition that would spawn two servers. While the second server would crash and not start,
                        // It's better for it to not appear at all. This delay helps avoid that.
                        await Task.Delay(20000);
                    }

                    var assemblyFolder = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
                    var logFolder = Path.GetFullPath(Path.Combine(assemblyFolder.Parent.Parent.Parent.FullName, "logs"));
                    Directory.CreateDirectory(logFolder);
                    var config = new Configuration() { Port = 59330, LogDirectory = logFolder };
                    await diag.StartAsync(config);
                });
            }

            // Finish setting up config
            if (!IsRunningInAzure() && HostingEnvironment.IsDevelopment() && AppSettings.GenerateLocalHostNameFromNgrok)
            {
                var ngrokHosted = app.ApplicationServices.GetService<NgrokHostedService>();

                // We need to first verify that the Ngrok process is running first, rather than
                // spin up a new session on our own, since that should be started with the frontend project.
                var isNgrokRunning = ngrokHosted.IsNgrokRunning().Result;
                if (!isNgrokRunning)
                {
                    throw new TimeoutException("Could not find existing Ngrok process. Did you start the frontend service?");
                }

                // Normally, the Ngrok service would start up after the application has started to launch.
                // We need it to start sooner than that, since we need the hostname in advance.
                ngrokHosted.OnApplicationStarted().Wait();
                var tunnels = ngrokHosted.GetTunnelsAsync().Result;
                ConfigureLocalHostname(new Uri(tunnels.First().PublicURL).Host);
            }

            // Swagger/OpenAPI
            app.UseSwagger(x =>
            {
                x.RouteTemplate = "{documentName}/swagger";
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
                // UI should be visible from /swagger endpoint. Note that leading slash
                // is not present on the RoutePrefix below. This is intentional, because it
                // doesn't work correctly if you add it. I think this is a bug in the Swashbuckle
                // Swagger UI implementation.
                c.RoutePrefix = "swagger";

                // The swagger json document is served up from /v1/swagger.
                c.SwaggerEndpoint("/v1/swagger", "BackendWebApi API v1");
                c.DisplayRequestDuration();
            });

            Warmup(app);
        }

        private static string RequiresNotNullOrEmpty(string value, string paramName)
        {
            Requires.NotNullOrEmpty(value, paramName);
            return value;
        }

        private static void RegisterUnhandledExceptionHandler(IDiagnosticsLogger logger)
        {
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                // In future we may take a heap dump of it here.
                logger.LogCritical($"Process terminating: {e.IsTerminating}\n {e.ExceptionObject.ToString()}");
            };
        }
    }
}
