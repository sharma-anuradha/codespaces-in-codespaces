﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Authentication;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.AspNetCore.Hosting;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.ControllerAccess;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    public class Startup
    {
        private static readonly TimeSpan CacheControl_MaxAgeValue = TimeSpan.FromHours(2);

        public Startup(IConfiguration configuration, IWebHostEnvironment hostEnvironment)
        {
            Configuration = configuration;
            HostEnvironment = hostEnvironment;
        }

        private IWebHostEnvironment HostEnvironment { get; }

        public AppSettings AppSettings { get; set; }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            IDiagnosticsLogger Logger = new JsonStdoutLogger(new LogValueSet());
            services.AddSingleton<IDiagnosticsLogger>(Logger);

            services.AddDistributedMemoryCache();

            services.AddSession(options => { });

            services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            services.AddControllersWithViews().AddControllersAsServices();
            services.AddRazorPages();

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            // Configuration
            var appSettingsConfiguration = Configuration.GetSection("AppSettings");
            var appSettings = appSettingsConfiguration.Get<AppSettings>();
            services.Configure<AppSettings>(appSettingsConfiguration);
            services.AddSingleton(appSettings);
            AppSettings = appSettings;

            if (string.IsNullOrEmpty(AppSettings.AesKey)
                 || string.IsNullOrEmpty(AppSettings.AesIV)
                 || string.IsNullOrEmpty(AppSettings.Domain))
            {
                throw new Exception("AesKey, AesIV or Domain keys are not found in the app settings.");
            }

            // VS SaaS Authentication
            services.AddPortalWebSiteAuthentication(HostEnvironment, appSettings);

            // Basic VS SaaS Hosting: health provider, in memory caching, logging, hosting environment, http context accessor, and key vault reader
            services.AddVsSaaSHosting(
                this.HostEnvironment, 
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
             
            // Forwarded headers
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            services.AddSingleton<IControllerProvider, ControllerProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

            if (env.IsDevelopment() || AppSettings.IsLocal)
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();

            app.UseSpaStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = (ctx) =>
                {
                    // TODO: Remove later.
                    ctx.Context.Response.Headers.Add("Service-Worker-Allowed", "/");
                }
            });
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                if (!context.Request.Path.StartsWithSegments("/static", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.GetTypedHeaders().CacheControl = 
                        new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                        {
                            Public = true,
                            MaxAge = CacheControl_MaxAgeValue
                        };
                }
                await next();
            });

            // VS SaaS Middleware: ApplicationServicesProvider, logging factory, x-content-type-options header, unhandled exception reporter, request ids, diagnostics, authentication
            app.UseVsSaaS(env.IsDevelopment(), useAuthentication: true);
                
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(routes =>
            {
                routes.MapControllerRoute("default", "{controller}/{action=Index}/{id?}");
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

            ClientKeyvaultReader.GetKeyvaultKeys().Wait();
        }
    }
}
