#if !DEBUG
// Only enable Azure in Release configuration
#define Azure_SignalR
#endif

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VsSaaS.AspNetCore.TelemetryProvider;

namespace Microsoft.VsCloudKernel.SignalService
{
    public interface IStartup
    {
        bool UseAzureSignalR { get; }
        ITokenValidationProvider TokenValidationProvider { get; }
        IConfigurationRoot Configuration { get; }
    }

    public class Startup : IStartup
    {
        private readonly ILogger<Startup> logger;

        /// <summary>
        /// Map to the presence hub signalR
        /// </summary>
        private const string PresenceHubMap = "/presencehub";

        public static bool AzureSignalREnabled =>
#if Azure_SignalR
           true;
#else
           false;
#endif

        public bool UseAzureSignalR { get; private set; }

        public ITokenValidationProvider TokenValidationProvider { get; private set; }

        private readonly IHostingEnvironment _hostEnvironment;

        public Startup(ILogger<Startup> logger,IHostingEnvironment env)
        {
            this.logger = logger;
            this.logger.LogInformation("Startup");

            this._hostEnvironment = env;

            // Build configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.secrets.json", optional: true)
#if DEBUG
                .AddJsonFile("appsettings.Development.json", optional: true)
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

            // register our logger instance
            services.AddTransient<ILogger>((srvcPrvoer) => this.logger);

            // Frameworks
            services.AddMvc();

            // Configuration
            var appSettingsConfiguration = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsConfiguration);

            TokenValidationProvider = LocalTokenValidationProvider.Create(appSettingsConfiguration);
            if (TokenValidationProvider == null )
            {
                // no local certificates were deployed so attempt to look on auth metadata Uri
                var authenticateMetadataServiceUri = appSettingsConfiguration.GetValue<string>(nameof(AppSettings.AuthenticateMetadataServiceUri));
                if (!string.IsNullOrEmpty(authenticateMetadataServiceUri))
                {
                    this.logger.LogInformation($"Using CertificateMetadataProvider:{authenticateMetadataServiceUri}");

                    TokenValidationProvider = new CertificateMetadataProviderService(authenticateMetadataServiceUri, this.logger);
                    services.AddSingleton((srvcProvider) => TokenValidationProvider as IHostedService);
                }
            }

            // Add Jwt authentication only if we have a token validator
            if (TokenValidationProvider != null)
            {
                this.logger.LogInformation("AddAuthenticationServices");
                services.AddAuthenticationServices(TokenValidationProvider, this.logger);
            }

            // Create the Azure Cosmos backplane provider service
            services.AddSingleton<IHostedService, DatabaseBackplaneProviderService>();

            // SignalR support
            services.AddSingleton<PresenceService>();

            var signalRService = services.AddSignalR();
            if (AzureSignalREnabled && Configuration.HasAzureSignalRConnections())
            {
                this.logger.LogInformation($"Add Azure SignalR");
                UseAzureSignalR = true;
                signalRService.AddAzureSignalR();
            }

            // IStartup
            services.AddSingleton<IStartup>(this);

            // If telemetry console provider is wanted
            if (appSettingsConfiguration.GetValue<bool>(nameof(AppSettings.UseTelemetryProvider)))
            {
                // inject the Telemetry logger provider
                services.ReplaceConsoleTelemetry();
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
                    if (TokenValidationProvider != null)
                    {
                        routes.MapHub<AuthorizedPresenceServiceHub>(PresenceHubMap);
                    }
                    else
                    {
                        routes.MapHub<PresenceServiceHub>(PresenceHubMap);
                    }
                });
            }
            else
            {
                // configure standalone SignalR service
                app.UseSignalR(routes =>
                {
                    if (TokenValidationProvider != null)
                    {
                        routes.MapHub<AuthorizedPresenceServiceHub>(PresenceHubMap);
                    }
                    else
                    {
                        routes.MapHub<PresenceServiceHub>(PresenceHubMap);
                    }
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