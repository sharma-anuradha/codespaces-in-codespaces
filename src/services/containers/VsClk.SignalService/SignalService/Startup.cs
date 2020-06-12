// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsCloudKernel.Services.Backplane.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Our main Startup class.
    /// </summary>
    public class Startup : StartupBase<AppSettings>
    {
        /// <summary>
        /// Map to the health hub signalR.
        /// </summary>
        internal const string HealthHubMap = "/healthhub";

        /// <summary>
        /// Map to the universal hub signalR.
        /// </summary>
        private const string SignalRHubMap = "/signalrhub";

        /// <summary>
        /// Map to the universal hub signalR.
        /// </summary>
        private const string SignalRHubDevMap = "/signalrhub-dev";

        /// <summary>
        /// Map to the presence hub signalR.
        /// </summary>
        private const string PresenceHubMap = "/presencehub";

        /// <summary>
        /// Map to dev presence service space hub.
        /// </summary>
        private const string PresenceHubDevMap = "/presencehub-dev";

        /// <summary>
        /// CORS policy name.
        /// </summary>
        private const string VsoCorsPolicy = "vsoCORSPolicy";

        /// <summary>
        /// Option to disable the auth.
        /// </summary>
        private const string NoAuthenticationOption = "noAuth";

        /// <summary>
        /// Option to disable backplane support.
        /// </summary>
        private const string NoBackplaneOption = "noBackplane";

        public Startup(
            IConfiguration configuration,
            IWebHostEnvironment hostEnvironment,
            ILoggerFactory loggerFactory)
            : base(configuration, hostEnvironment, loggerFactory)
        {
        }

        public bool UseAzureSignalR { get; private set; }

        public bool EnableAuthentication { get; private set; }

        public override string ServiceType => "SignalR";

        protected override Type AppType => typeof(Startup);

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureCommonServices(services);

            // define CORS
            var corsOrigins = AppSettingsConfiguration.GetSection(nameof(AppSettings.CorsOrigin))
                .GetChildren()
                .Select(x => x.Value)
                .ToArray();
            services.AddCors(options =>
            {
                // define VSO Cors policy
                options.AddPolicy(
                    VsoCorsPolicy,
                    builder =>
                    {
                        builder
                            .WithOrigins(corsOrigins.ToArray())
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    });
            });

            // Frameworks
            services.AddControllers();

            // provide IHttpClientFactory
            services.AddHttpClient();

            // Next block will enable authentication based on a Profile service Uri
            var authenticateProfileServiceUri = AppSettingsConfiguration.GetValue<string>(nameof(AppSettings.AuthenticateProfileServiceUri));
            if (!GetBoolConfiguration(NoAuthenticationOption) && !string.IsNullOrEmpty(authenticateProfileServiceUri))
            {
                Logger.LogInformation("Authentication enabled...");

                EnableAuthentication = true;
                services
                    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer();
                services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerOptionsPostConfigureOptions>();
            }

            if (!GetBoolConfiguration(NoBackplaneOption))
            {
                var backplaneHostName = AppSettingsConfiguration.GetValue<string>(nameof(AppSettings.BackplaneHostName));
                if (!string.IsNullOrEmpty(backplaneHostName))
                {
                    // add json Rpc backplane support
                    services.AddHostedService<JsonRpcBackplaneServiceProviderService<IContactBackplaneManager, ContactBackplaneServiceProvider>>();
                    services.AddHostedService<JsonRpcBackplaneServiceProviderService<IRelayBackplaneManager, RelayBackplaneServiceProvider>>();
                }
                else
                {
                    // Create the Azure Cosmos backplane provider service
                    services.AddSingleton<IAzureDocumentsProviderServiceFactory, AzureDocumentsProviderFactory>();
                    services.AddHostedService<AzureDocumentsProviderService>();

                    // Create the Azure Redis backplane provider service
                    services.AddSingleton<IAzureRedisProviderServiceFactory, AzureRedisContactsProviderFactory>();
                    services.AddSingleton<IAzureRedisProviderServiceFactory, AzureRedisRelayProviderFactory>();
                    services.AddHostedService<AzureRedisProviderService>();
                }
            }

            // Service options
            services.AddSingleton((srvcProvider) => new HubServiceOptions() { Id = ServiceId, Stamp = Stamp });

            // SignalR support services
            services.AddSingleton<ContactService>();

            services.AddSingleton<RelayService>();

            var signalRService = services.AddSignalR()
                .AddNewtonsoftJsonProtocol()
                .AddMessagePackProtocol((options) =>
                {
                });

            var keyVaultName = AppSettingsConfiguration.GetValue<string>(nameof(AppSettings.KeyVaultName));

            // if we can eventually retrieve signalR endpoints from the key vault
            var canRetrieveKeyVaultSignalREndpoints =
                !string.IsNullOrEmpty(ServicePrincipal?.ClientId) &&
                !string.IsNullOrEmpty(ServicePrincipal?.ClientPassword) &&
                !string.IsNullOrEmpty(keyVaultName) &&
                !string.IsNullOrEmpty(Stamp);

            var serviceEndpoints = new List<ServiceEndpoint>();

            // inject the list of available endpoints
            services.AddSingleton<IList<ServiceEndpoint>>((srvcProvider) => serviceEndpoints);

            if (Configuration.HasAzureSignalRConnections() || canRetrieveKeyVaultSignalREndpoints)
            {
                Logger.LogInformation($"Add Azure SignalR");
                if (canRetrieveKeyVaultSignalREndpoints)
                {
                    try
                    {
                        serviceEndpoints.AddRange(ServicePrincipal.GetAzureSignalRServiceEndpointsAsync(keyVaultName, Stamp).Result);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, $"Failed to retrieve endpoints from Azure key vault:{keyVaultName}");
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
            services.AddSingleton(new HubDispatcher(ContactServiceHub.HubContextName, EnableAuthentication ? typeof(AuthorizedContactServiceHub) : typeof(ContactServiceHub)));
            services.AddSingleton(new HubDispatcher(RelayServiceHub.HubContextName, EnableAuthentication ? typeof(AuthorizedRelayServiceHub) : typeof(RelayServiceHub)));

            // hub context hosts definition
            if (EnableAuthentication)
            {
                // support for custom presence endpoint
                services.AddSingleton<IHubContextHost, HubContextHost<ContactServiceHub, AuthorizedContactServiceHub>>();

                // universal hub supported contexts
                services.AddSingleton<IHubContextHost, SignalRHubContextHost<ContactServiceHub, AuthorizedSignalRHub>>();
                services.AddSingleton<IHubContextHost, SignalRHubContextHost<RelayServiceHub, AuthorizedSignalRHub>>();
            }

            if (IsDevelopmentEnv || !EnableAuthentication)
            {
                services.AddSingleton<IHubContextHost, HubContextHost<ContactServiceHub, ContactServiceHub>>();

                services.AddSingleton<IHubContextHost, SignalRHubContextHost<ContactServiceHub, SignalRHub>>();
                services.AddSingleton<IHubContextHost, SignalRHubContextHost<RelayServiceHub, SignalRHub>>();
            }

            // Host for RelayService
            services.AddHostedService<DisposableHostedService<RelayService>>();

            // backplane manager support
            services.AddSingleton<ContactBackplaneManager>();
            services.AddSingleton<IContactBackplaneManager>(srvcProvider => srvcProvider.GetRequiredService<ContactBackplaneManager>());
            services.AddSingleton<IBackplaneManagerBase>(srvcProvider => srvcProvider.GetRequiredService<ContactBackplaneManager>());

            services.AddSingleton<RelayBackplaneManager>();
            services.AddSingleton<IRelayBackplaneManager>(srvcProvider => srvcProvider.GetRequiredService<RelayBackplaneManager>());
            services.AddSingleton<IBackplaneManagerBase>(srvcProvider => srvcProvider.GetRequiredService<RelayBackplaneManager>());

            services.AddHostedService<BackplaneManagerHostedService>();

            // define long running health echo provider
            services.AddHostedService<SignalRHealthStatusProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Authentication
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseFileServer();

            // activate VSO Cors policy
            app.UseCors(VsoCorsPolicy);

            // SignalR configure
            if (UseAzureSignalR)
            {
                Logger.LogInformation($"Using Azure SignalR");

                // configure Azure SignalR service
                app.UseAzureSignalR(routes =>
                {
                    if (EnableAuthentication)
                    {
                        routes.MapHub<AuthorizedSignalRHub>(SignalRHubMap);
                        routes.MapHub<AuthorizedContactServiceHub>(PresenceHubMap);
                        if (IsDevelopmentEnv)
                        {
                            routes.MapHub<SignalRHub>(SignalRHubDevMap);
                            routes.MapHub<ContactServiceHub>(PresenceHubDevMap);
                        }
                    }
                    else
                    {
                        routes.MapHub<SignalRHub>(SignalRHubMap);
                        routes.MapHub<ContactServiceHub>(PresenceHubMap);
                    }

                    routes.MapHub<HealthServiceHub>(HealthHubMap);
                });
            }

            app.UseEndpoints(routes =>
            {
                if (!UseAzureSignalR)
                {
                    if (EnableAuthentication)
                    {
                        routes.MapHub<AuthorizedSignalRHub>(SignalRHubMap);
                        routes.MapHub<AuthorizedContactServiceHub>(PresenceHubMap);
                        if (IsDevelopmentEnv)
                        {
                            routes.MapHub<SignalRHub>(SignalRHubDevMap);
                            routes.MapHub<ContactServiceHub>(PresenceHubDevMap);
                        }
                    }
                    else
                    {
                        routes.MapHub<SignalRHub>(SignalRHubMap);
                        routes.MapHub<ContactServiceHub>(PresenceHubMap);
                    }

                    routes.MapHub<HealthServiceHub>(HealthHubMap);
                }

                routes.MapControllerRoute("default", "{controller=Default}/{action=Get}");
            });
        }
    }
}