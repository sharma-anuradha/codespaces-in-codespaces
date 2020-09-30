using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.CodespacesApiClient;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    public class Startup
    {
        private static readonly TimeSpan CacheControl_YearValue = TimeSpan.FromDays(365);
        private static readonly PathString[] ExcludedPortForwardingPaths = new PathString[]
        {
            PathString.FromUriComponent("/auth"),
            PathString.FromUriComponent("/authenticate-workspace"),
            PathString.FromUriComponent("/authenticate-codespace"),
        };

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
            services.AddTransient(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetService<IDiagnosticsLoggerFactory>();
                var logValueSet = serviceProvider.GetService<LogValueSet>();
                return loggerFactory.New(logValueSet);
            });

            services.AddDistributedMemoryCache();

            services.AddSession(options => { });

            services.AddCors();

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

            services.AddSpaStaticFiles(configuration => { configuration.RootPath = "ClientApp/build"; });

            PortForwardingHostUtils = new PortForwardingHostUtils(appSettings.PortForwardingHostsConfigs);
            services.AddSingleton(PortForwardingHostUtils);
            PortForwardingRoutingHelper = new PortForwardingRoutingHelper(PortForwardingHostUtils);

            services.AddHttpCodespacesApiClient((options) =>
            {
                options.BaseAddress = AppSettings.PortalEndpoint;
                options.ServiceName = "VSCodespacesPortal";
                options.Version = typeof(Startup).Assembly.GetName().Version!.ToString();
            });

            services.AddSingleton<IWorkspaceInfo, WorkspaceInfo>();
            services.AddSingleton<ICookieEncryptionUtils, CookieEncryptionUtils>();
            services.AddHttpClient<ILiveShareTokenExchangeUtil, LiveShareTokenExchangeUtil>();

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

            services.AddSingleton<AsyncWarmupHelper>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors(options => options.WithOrigins("https://portal.azure.com", "https://df.onecloud.azure-test.net").AllowAnyHeader());

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
            });

            if (HostEnvironment.IsDevelopment() || AppSettings.IsLocal)
            {
                app.UseDeveloperExceptionPage();
            }

            if (AppSettings.IsLocal)
            {
                // Enable PII data in logs for Local
                IdentityModelEventSource.ShowPII = true;
            }

            app.Use(async (httpContext, next) =>
            {
                if (httpContext.Request.Headers.TryGetValue("X-Request-ID", out var nginxRequestId))
                {
                    var requestId = nginxRequestId.SingleOrDefault();
                    if (requestId != default)
                    {
                        httpContext.TrySetRequestId(requestId);
                    }
                }

                await next();
            });

            // First we try to get correlation id from query string param cid.
            // We don't override existing correlation id headers though. That would be bad.
            app.Use(async (httpContext, next) =>
            {
                if (!httpContext.Request.Headers.TryGetValue(HttpConstants.CorrelationIdHeader, out var correlationIdHeader) ||
                    string.IsNullOrEmpty(correlationIdHeader))
                {
                    if (httpContext.Request.Query.TryGetValue("cid", out var requestCorrelationId))
                    {
                        var correlationId = requestCorrelationId.SingleOrDefault();
                        if (!string.IsNullOrWhiteSpace(correlationId))
                        {
                            // VsSaaS SDK middleware overrides correlation id by this header or request id.
                            httpContext.Request.Headers.TryAdd(HttpConstants.CorrelationIdHeader, correlationId);
                        }
                    }
                }

                await next();
            });

            // Then we try to get correlation id from a cookie.
            // We don't override existing correlation id headers though. Still bad.
            app.Use(async (httpContext, next) =>
            {
                if (!httpContext.Request.Headers.TryGetValue(HttpConstants.CorrelationIdHeader, out var correlationIdHeader) ||
                    string.IsNullOrEmpty(correlationIdHeader))
                {
                    if (httpContext.Request.Cookies.TryGetValue("codespaces_correlation_id", out var cookieCorrelationId) &&
                        !string.IsNullOrWhiteSpace(cookieCorrelationId))
                    {
                        // VsSaaS SDK middleware overrides correlation id by this header or request id.
                        httpContext.Request.Headers.TryAdd(HttpConstants.CorrelationIdHeader, cookieCorrelationId);
                    }
                }

                await next();
            });

            app.Use(async (context, next) =>
            {
                // Locally the webpack dev server serves assets under constant names and /static/js|css|.../* paths. We don't want the cashing locally.
                if (AppSettings.IsLocal ||
                    !context.Request.Path.StartsWithSegments("/static", StringComparison.OrdinalIgnoreCase) &&
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

            app.MapWhen(Not(IsPortForwardingRequest), ConfigurePortal);
            app.MapWhen(IsPortForwardingRequest, ConfigurePortForwarding);

            InitSecrets(app);
        }

        private void InitSecrets(IApplicationBuilder app)
        {
            if (AppSettings.IsLocal == true)
            {
                ClientKeyvaultReader.SetLocalKeychainKeys();

                try
                {
                    app.ApplicationServices.GetRequiredService<AsyncWarmupHelper>().RunAsync().Wait();
                }
                catch (Exception e)
                {
                    if (!(e.InnerException is AdalServiceException))
                    {
                        throw e;
                    }
                }
            }
            else
            {
                ClientKeyvaultReader.GetKeyvaultKeys().Wait();
                app.ApplicationServices.GetRequiredService<AsyncWarmupHelper>().RunAsync().Wait();
            }
        }

        private bool IsPortForwardingRequest(HttpContext context)
        {
            if (ExcludedPortForwardingPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
            {
                return false;
            }

            return PortForwardingRoutingHelper.IsPortForwardingRequest(context);
        }

        private void ConfigurePortal(IApplicationBuilder app)
        {
            app.UseResponseCompression();

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

            // VS SaaS Middleware: ApplicationServicesProvider, logging factory, x-content-type-options header, unhandled exception reporter, request ids, diagnostics, authentication
            app.UseVsSaaS(HostEnvironment.IsDevelopment(), useAuthentication: true);

            app.Use(async (context, next) =>
            {
                var logger = context.GetLogger();
                var endpoint = context.GetEndpoint();

                logger.AddBaseValue("routing_context", "Portal");
                logger.AddBaseValue("endpoint", endpoint?.DisplayName);
                if (context.Request.Headers.TryGetValue(PortForwardingHeaders.OriginalUrl, out var originalUrlValues))
                {
                    logger.AddBaseValue("HttpOriginalUrl", originalUrlValues.ToString());
                }

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
                // don't use the dev server proxy is running in the "platform" mode locally
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
                var spaPath = Path.Combine(HostEnvironment.ContentRootPath, "ClientApp", "build");

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(spaPath)
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
                if (context.Request.Headers.TryGetValue(PortForwardingHeaders.OriginalUrl, out var originalUrlValues))
                {
                    logger.AddBaseValue("HttpOriginalUrl", originalUrlValues.ToString());
                }

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
