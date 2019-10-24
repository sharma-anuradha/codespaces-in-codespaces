using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VsSaaS.AspNetCore.TelemetryProvider;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.SignalService
{
    public interface IStartup
    {
        bool UseAzureSignalR { get; }
        bool EnableAuthentication { get; }
        string Environment { get; }
        IConfigurationRoot Configuration { get; }
        bool IsDevelopmentEnv { get; }
    }

    public class Startup : IStartup
    {
        Func<Type, ILogger> loggerFactory;
        private ILogger logger;

        /// <summary>
        /// Map to the universal hub signalR
        /// </summary>
        private const string SignalRHubMap = "/signalrhub";

        /// <summary>
        /// Map to the universal hub signalR
        /// </summary>
        private const string SignalRHubDevMap = "/signalrhub-dev";

        /// <summary>
        /// Map to the presence hub signalR
        /// </summary>
        private const string PresenceHubMap = "/presencehub";

        /// <summary>
        /// Map to dev presence service space hub
        /// </summary>
        private const string PresenceHubDevMap = "/presencehub-dev";

        /// <summary>
        /// Map to the health hub signalR
        /// </summary>
        internal const string HealthHubMap = "/healthhub";

        #region IStartup

        public bool UseAzureSignalR { get; private set; }
        public bool EnableAuthentication { get; private set; }
        public string Environment => this._hostEnvironment.EnvironmentName;
        public bool IsDevelopmentEnv => this._hostEnvironment.IsDevelopment();

        #endregion

        private readonly IHostingEnvironment _hostEnvironment;


        public Startup(ILoggerFactory loggerFactory,IHostingEnvironment env)
        {
            this.loggerFactory = (t) => loggerFactory.CreateLogger(t.FullName);

            this._hostEnvironment = env;

            // Build configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddJsonFile("appsettings.Debug.json", optional: true)
#else
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
#endif
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Configuration
            var appSettingsConfiguration = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsConfiguration);

            // create a unique service Id
            var serviceId = Guid.NewGuid().ToString();

            // define the stamp
            var stamp = appSettingsConfiguration.GetValue<string>(nameof(AppSettings.Stamp));

            // If telemetry console provider is wanted
            if (appSettingsConfiguration.GetValue<bool>(nameof(AppSettings.UseTelemetryProvider)))
            {
                // inject the Telemetry logger provider
                services.ReplaceConsoleTelemetry((opts) =>
                {
                    // Our options on every telemetry log
                    opts.FactoryOptions = new Dictionary<string, object>()
                    {
                        { "Service", "signlr" },
                        { "ServiceId", serviceId },
                        { "Stamp", stamp }
                    };
                });

                var serviceProvider = services.BuildServiceProvider();
                this.loggerFactory = (t) => serviceProvider.GetService(t) as ILogger;
            }

            this.logger = this.loggerFactory(typeof(ILogger<Startup>));
            this.logger.LogInformation($"ConfigureServices -> env:{this._hostEnvironment.EnvironmentName}");

            // Frameworks
            services.AddMvc()
#if _NETCORE3_
                .AddMvcOptions(options => options.EnableEndpointRouting = false)
#endif
            ;

            // provide IHttpClientFactory
            services.AddHttpClient();

            // define list of IAsyncWarmup implementation available
            var warmupServices = new List<IAsyncWarmup>();
            services.AddSingleton<IList<IAsyncWarmup>>((srvcProvider) => warmupServices);

            // define list of IHealthStatusProvider implementation available
            var healthStatusProviders = new List<IHealthStatusProvider>();
            services.AddSingleton<IList<IHealthStatusProvider>>((srvcProvider) => healthStatusProviders);

            // define our overall warmup service
            services.AddSingleton<WarmupService>();

            // define our overall health service
            services.AddSingleton<HealthService>();

            // Next block will enable authentication based on a Profile service Uri
            var authenticateProfileServiceUri = appSettingsConfiguration.GetValue<string>(nameof(AppSettings.AuthenticateProfileServiceUri));
            if (!string.IsNullOrEmpty(authenticateProfileServiceUri))
            {            
                this.logger.LogInformation("Authentication enabled...");

                EnableAuthentication = true;
                services.AddProfileServiceJwtBearer(
                    authenticateProfileServiceUri,
                    this.loggerFactory(typeof(ILogger<Authenticate>)),
                    $"signlr-{serviceId}-{stamp}");
            }

            // Create the Azure Cosmos backplane provider service
            services.AddHostedService<AzureDocumentsProviderService>();

            // Create the Azure Redis backplane provider service
            services.AddHostedService<AzureRedisProviderService>();

            // Service options
            services.AddSingleton((srvcProvider) => new PresenceServiceOptions() { Id = serviceId });
            services.AddSingleton((srvcProvider) => new RelayServiceOptions() { Id = serviceId });

            if (appSettingsConfiguration.GetValue<bool>(nameof(AppSettings.IsPrivacyEnabled)))
            {
                this.logger.LogInformation("Privacy enabled...");
                // define our IHubFormatProvider
                services.AddSingleton<IHubFormatProvider, DataFormatter>();
            }

            // SignalR support services
            services.AddSingleton<PresenceService>();
            services.AddSingleton<RelayService>();

            var signalRService = services.AddSignalR().AddJsonProtocol(options => {
#if _NETCORE3_
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
#else
                // ensure we disable the camel case contract
                options.PayloadSerializerSettings.ContractResolver =
                new Newtonsoft.Json.Serialization.DefaultContractResolver();
#endif
            });

            // DI for ApplicationServicePrincipal
            var applicationServicePrincipal = Configuration.GetSection(nameof(ApplicationServicePrincipal)).Get<ApplicationServicePrincipal>();
            services.AddSingleton((srvcProvider) => applicationServicePrincipal);

            var keyVaultName = appSettingsConfiguration.GetValue<string>(nameof(AppSettings.KeyVaultName));
            // if we can eventually retrieve signalR endpoints from the key vault
            var canRetrieveKeyVaultSignalREndpoints =
                !string.IsNullOrEmpty(applicationServicePrincipal?.ClientId) &&
                !string.IsNullOrEmpty(applicationServicePrincipal?.ClientPassword) &&
                !string.IsNullOrEmpty(keyVaultName) &&
                !string.IsNullOrEmpty(stamp);

            var serviceEndpoints = new List<ServiceEndpoint>();
            // inject the list of available endpoints
            services.AddSingleton<IList<ServiceEndpoint>>((srvcProvider) => serviceEndpoints);

            if (Configuration.HasAzureSignalRConnections() || canRetrieveKeyVaultSignalREndpoints)
            {
                this.logger.LogInformation($"Add Azure SignalR");
                if (canRetrieveKeyVaultSignalREndpoints)
                {
                    try
                    {
                        serviceEndpoints.AddRange(applicationServicePrincipal.GetAzureSignalRServiceEndpointsAsync(keyVaultName, stamp).Result);
                    }
                    catch(Exception e)
                    {
                        this.logger.LogError(e, $"Failed to retrieve endpoints from Azure key vault:{keyVaultName}");
                    }
                }

                if (Configuration.HasAzureSignalRConnections() || serviceEndpoints.Count > 0)
                {
                    UseAzureSignalR = true;
                    signalRService.AddAzureSignalR((serviceOptions) =>
                    {
                        // add the endpoints being configured by the env vars
                        var appSettingEndpoints = Configuration.GetAzureSignalRServiceEndpoints();
                        serviceEndpoints.AddRange(appSettingEndpoints.Where(e => serviceEndpoints.FindIndex(se => se.ConnectionString == e.ConnectionString) == -1).ToArray());
                        // now define the combined endpoints
                        serviceOptions.Endpoints = serviceEndpoints.ToArray();
                    });
                }
            }

            // support dispatching for universal signalR hub
            services.AddSingleton(new HubDispatcher(PresenceServiceHub.Name, EnableAuthentication ? typeof(AuthorizedPresenceServiceHub) : typeof(PresenceServiceHub)));
            services.AddSingleton(new HubDispatcher(RelayServiceHub.Name, EnableAuthentication ? typeof(AuthorizedRelayServiceHub) : typeof(RelayServiceHub)));

            // hub context hosts definition
            if (EnableAuthentication)
            {
                // support for custom presence endpoint
                services.AddSingleton<IHubContextHost, HubContextHost<PresenceServiceHub, AuthorizedPresenceServiceHub>>();

                // universal hub supported contexts
                services.AddSingleton<IHubContextHost, SignalRHubContextHost<PresenceServiceHub, AuthorizedSignalRHub>>();
                services.AddSingleton<IHubContextHost, SignalRHubContextHost<RelayServiceHub, AuthorizedSignalRHub>>();
            }

            if (IsDevelopmentEnv || !EnableAuthentication)
            {
                services.AddSingleton<IHubContextHost, HubContextHost<PresenceServiceHub, PresenceServiceHub>>();

                services.AddSingleton<IHubContextHost, SignalRHubContextHost<PresenceServiceHub, SignalRHub>>();
                services.AddSingleton<IHubContextHost, SignalRHubContextHost<RelayServiceHub, SignalRHub>>();
            }

            // a background service to control lifetime of the presence service
            services.AddHostedService<PresenceBackgroundService>();

            // define long running health echo provider
            services.AddHostedService<SignalRHealthStatusProvider>();

            // IStartup
            services.AddSingleton<IStartup>(this);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Authentication
            app.UseAuthentication();

            app.UseFileServer();

            // SignalR configure
            if (UseAzureSignalR)
            {
                this.logger.LogInformation($"Using Azure SignalR");

                // configure Azure SignalR service
                app.UseAzureSignalR(routes =>
                {
                    if (EnableAuthentication)
                    {
                        routes.MapHub<AuthorizedSignalRHub>(SignalRHubMap);
                        routes.MapHub<AuthorizedPresenceServiceHub>(PresenceHubMap);
                        if (IsDevelopmentEnv)
                        {
                            routes.MapHub<PresenceServiceHub>(PresenceHubDevMap);
                            routes.MapHub<SignalRHub>(SignalRHubDevMap);
                        }
                    }
                    else
                    {
                        routes.MapHub<SignalRHub>(SignalRHubMap);
                        routes.MapHub<PresenceServiceHub>(PresenceHubMap);
                    }

                    routes.MapHub<HealthServiceHub>(HealthHubMap);
                });
            }
            else
            {
                // configure standalone SignalR service
                app.UseSignalR(routes =>
                {
                    if (EnableAuthentication)
                    {
                        routes.MapHub<AuthorizedSignalRHub>(SignalRHubMap);
                        routes.MapHub<AuthorizedPresenceServiceHub>(PresenceHubMap);
                        if (IsDevelopmentEnv)
                        {
                            routes.MapHub<PresenceServiceHub>(PresenceHubDevMap);
                            routes.MapHub<SignalRHub>(SignalRHubDevMap);
                        }
                    }
                    else
                    {
                        routes.MapHub<SignalRHub>(SignalRHubMap);
                        routes.MapHub<PresenceServiceHub>(PresenceHubMap);
                    }

                    routes.MapHub<HealthServiceHub>(HealthHubMap);
                });
            }

            // Frameworks
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                name: "default",
                template: "{controller=Status}/{action=Get}");
            });
        }
    }
}