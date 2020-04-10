using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Authentication;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.AspNetCore.Hosting;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Routing;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    public class Startup
    {
        private static readonly TimeSpan CacheControl_MaxAgeValue = TimeSpan.FromHours(2);
        private static readonly TimeSpan CacheControl_YearValue = TimeSpan.FromDays(365);

        public Startup(IConfiguration configuration, IWebHostEnvironment hostEnvironment)
        {
            Configuration = configuration;
            HostEnvironment = hostEnvironment;
        }

        private IWebHostEnvironment HostEnvironment { get; }

        private AppSettings AppSettings { get; set; }

        private IConfiguration Configuration { get; }

        private PortForwardingHostUtils PortForwardingHostUtils { get; set; }

        private PortForwardingRoutingHelper PortForwardingRoutingHelper { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            IDiagnosticsLogger logger = new JsonStdoutLogger(new LogValueSet());
            services.AddSingleton<IDiagnosticsLogger>(logger);

            services.AddDistributedMemoryCache();

            services.AddSession(options => { });

            services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            services.AddControllersWithViews().AddControllersAsServices();

            // Configuration
            var appSettingsConfiguration = Configuration.GetSection("AppSettings");
            var appSettings = appSettingsConfiguration.Get<AppSettings>();
            services.Configure<AppSettings>(appSettingsConfiguration);
            services.AddSingleton(appSettings);
            AppSettings = appSettings;

            // In production, the React files will be served from this directory
            // In tests, the assets are not built, but the static index.html will do just fine.
            var spaRootPath = AppSettings.IsTest ? "ClientApp/public" : "ClientApp/build";
            services.AddSpaStaticFiles(configuration => { configuration.RootPath = spaRootPath; });

            PortForwardingHostUtils = new PortForwardingHostUtils(appSettings.PortForwardingHostsConfigs);
            services.AddSingleton(PortForwardingHostUtils);
            PortForwardingRoutingHelper = new PortForwardingRoutingHelper(PortForwardingHostUtils);

            if (string.IsNullOrEmpty(AppSettings.AesKey)
                || string.IsNullOrEmpty(AppSettings.AesIV)
                || string.IsNullOrEmpty(AppSettings.Domain))
            {
                throw new Exception("AesKey, AesIV or Domain keys are not found in the app settings.");
            }

            // Basic VS SaaS Hosting: health provider, in memory caching, logging, hosting environment, http context accessor, and key vault reader
            services.AddVsSaaSHosting(
                HostEnvironment,
                new LoggingBaseValues
                {
                    ServiceName = "vso_portal",
                    CommitId = null,
                },
                configureSecretReaderOptions: (options) =>
                {
                    options.ServicePrincipalClientId = appSettings.KeyVaultReaderServicePrincipalClientId;
                    options.GetServicePrincipalClientSecretAsyncCallback = () => Task.FromResult(appSettings.KeyVaultReaderServicePrincipalClientSecret);
                });

            // VS SaaS Authentication
            services.AddPortalWebSiteAuthentication(appSettings);

            // Forwarded headers
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            services.AddSingleton<AsyncWarmupHelper>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (HostEnvironment.IsDevelopment() || AppSettings.IsLocal)
            {
                app.UseDeveloperExceptionPage();
            }

            if (AppSettings.IsLocal)
            {
                // Enable PII data in logs for Local
                IdentityModelEventSource.ShowPII = true;
            }

            app.MapWhen(Not(IsPortForwardingRequest), ConfigurePortal);
            app.MapWhen(IsPortForwardingRequest, ConfigurePortForwarding);

            ClientKeyvaultReader.GetKeyvaultKeys().Wait();
            app.ApplicationServices.GetRequiredService<AsyncWarmupHelper>().RunAsync().Wait();
        }

        private bool IsPortForwardingRequest(HttpContext context)
        {
            return PortForwardingRoutingHelper.IsPortForwardingRequest(context);
        }

        private void ConfigurePortal(IApplicationBuilder app)
        {
            app.UseResponseCompression();

            app.UseForwardedHeaders();
            // TODO: the following is temporary until we get forwarded headers properly configured in nginx
            // For the short term this is safe as we know all public traffic is https
            // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-3.1
            if (!HostEnvironment.IsDevelopment() || !AppSettings.IsLocal)
            {
                app.Use((context, next) =>
                {
                    context.Request.Scheme = "https";
                    return next();
                });
            }

            if (!HostEnvironment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseSession();

            app.UseSpaStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = (ctx) =>
                {
                    ctx.Context.Response.Headers.Add("Service-Worker-Allowed", "/");
                }
            });
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                // Locally the webpack dev server serves assets under constant names and /static/js|css|.../* paths. We don't want the cashing locally.
                if (AppSettings.IsLocal ||
                    !context.Request.Path.StartsWithSegments("/static", StringComparison.OrdinalIgnoreCase) ||
                    !context.Request.Path.StartsWithSegments("/workbench-page", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.GetTypedHeaders().CacheControl =
                        new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                        {
                            Public = true,
                            NoCache = true,
                            NoStore = true,
                        };
                }
                else
                {
                    context.Response.GetTypedHeaders().CacheControl =
                        new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                        {
                            Public = true,
                            MaxAge = CacheControl_YearValue
                        };
                }

                await next();
            });

            // VS SaaS Middleware: ApplicationServicesProvider, logging factory, x-content-type-options header, unhandled exception reporter, request ids, diagnostics, authentication
            app.UseVsSaaS(HostEnvironment.IsDevelopment(), useAuthentication: true);

            app.Use(async (context, next) =>
            {
                var logger = context.GetLogger();
                var endpoint = context.GetEndpoint();

                logger.AddBaseValue("routing_context", "Portal");
                logger.AddBaseValue("endpoint", endpoint?.DisplayName);

                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(routes =>
            {
                routes.MapControllers();
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";
                if (AppSettings.IsLocal)
                {
                    // For development purposes, uncomment out if you want dotnet to load your react dev server, otherwise run 'yarn start' inside ClientApp
                    // spa.UseReactDevelopmentServer(npmScript: "start");
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:3030");
                }
            });
        }

        private void ConfigurePortForwarding(IApplicationBuilder app)
        {
            if (!AppSettings.IsTest && !AppSettings.IsLocal)
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(
                        Path.Combine(HostEnvironment.ContentRootPath, "ClientApp", "build"))
                });
            }

            if (AppSettings.IsTest || AppSettings.IsLocal)
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(
                        Path.Combine(HostEnvironment.ContentRootPath, "ClientApp", "public"))
                });
            }

            app.UseRouting();

            // VS SaaS Middleware: ApplicationServicesProvider, logging factory, x-content-type-options header, unhandled exception reporter, request ids, diagnostics, authentication
            app.UseVsSaaS(HostEnvironment.IsDevelopment(), useAuthentication: false);

            app.Use(async (context, next) =>
            {
                var logger = context.GetLogger();
                var endpoint = context.GetEndpoint();

                logger.AddBaseValue("routing_context", "PortForwarding");
                logger.AddBaseValue("endpoint", endpoint?.DisplayName);

                await next();
            });

            app.UseEndpoints(routes =>
            {
                routes.MapFallbackToController(
                    pattern: "{*path}",
                    action: nameof(PortForwarderController.Index),
                    controller: "PortForwarder");
            });
        }

        private Func<HttpContext, bool> Not(Func<HttpContext, bool> predicate)
        {
            return (arg) => !predicate(arg);
        }
    }
}