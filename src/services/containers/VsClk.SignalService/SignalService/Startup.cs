#if !DEBUG
// Only enable Azure in Release configuration
#define Azure_SignalR
#endif

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
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
    }

    public class Startup : IStartup
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger logger;

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

        public static bool AzureSignalREnabled =>
#if Azure_SignalR
           true;
#else
           false;
#endif

        public bool UseAzureSignalR { get; private set; }
        public bool EnableAuthentication { get; private set; }
        public string Environment => this._hostEnvironment.EnvironmentName;

        private readonly IHostingEnvironment _hostEnvironment;

        private bool IsDevelopmentEnv => this._hostEnvironment.EnvironmentName.Equals("Development");

        public Startup(ILoggerFactory loggerFactory,IHostingEnvironment env)
        {
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger(typeof(Startup).FullName);
            this.logger.LogInformation($"Startup -> env:{env.EnvironmentName}");

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
            this.logger.LogInformation("ConfigureServices");

            // Frameworks
            services.AddMvc();

            // Configuration
            var appSettingsConfiguration = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsConfiguration);

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
                services.AddProfileServiceJwtBearer(authenticateProfileServiceUri, this.loggerFactory.CreateLogger(typeof(AuthenticateProfileServiceExtension).FullName));
            }

            // Create the Azure Cosmos backplane provider service
            services.AddHostedService<DatabaseBackplaneProviderService>();

            var serviceId = Guid.NewGuid().ToString();

            // Presence Service options
            services.AddSingleton((srvcProvider) => new PresenceServiceOptions() { Id = serviceId });

            // SignalR support
            services.AddSingleton<PresenceService>();

            var signalRService = services.AddSignalR().AddJsonProtocol(options => {
                // ensure we disable the camel case contract
                options.PayloadSerializerSettings.ContractResolver =
                new Newtonsoft.Json.Serialization.DefaultContractResolver();
            });

            if (AzureSignalREnabled && Configuration.HasAzureSignalRConnections())
            {
                this.logger.LogInformation($"Add Azure SignalR");
                UseAzureSignalR = true;
                signalRService.AddAzureSignalR();
            }

            // a background service to control lifetime of the  presence service
            services.AddHostedService<PresenceBackgroundService>();

            // define long running health echo provider
            services.AddHostedService<SignalRHealthStatusProvider>();

            // IStartup
            services.AddSingleton<IStartup>(this);

            // If telemetry console provider is wanted
            if (appSettingsConfiguration.GetValue<bool>(nameof(AppSettings.UseTelemetryProvider)))
            {
                // inject the Telemetry logger provider
                services.ReplaceConsoleTelemetry((opts) =>
                {
                    opts.FactoryOptions = new Dictionary<string, object>()
                    {
                        { "Service", "signlr" },
                        { "ServiceId", serviceId },
                        { "Stamp", appSettingsConfiguration.GetValue<string>(nameof(AppSettings.Stamp))}
                    };
                });
            }
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
                        routes.MapHub<AuthorizedPresenceServiceHub>(PresenceHubMap);
                        if (IsDevelopmentEnv)
                        {
                            routes.MapHub<PresenceServiceHub>(PresenceHubDevMap);
                        }
                    }
                    else
                    {
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
                        routes.MapHub<AuthorizedPresenceServiceHub>(PresenceHubMap);
                        if (IsDevelopmentEnv)
                        {
                            routes.MapHub<PresenceServiceHub>(PresenceHubDevMap);
                        }
                    }
                    else
                    {
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