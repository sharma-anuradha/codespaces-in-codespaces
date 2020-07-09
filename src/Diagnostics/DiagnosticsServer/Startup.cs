// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using DiagnosticsServer.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Utilities;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Workers;

namespace DiagnosticsServer
{
    /// <summary>
    /// Configures the ASP.NET Core pipeline for HTTP requests.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// This method gets called by the runtime.
        /// Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The services collection that should be configured.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddSignalR();

            var logHubMessageHandler = new LogHubMessageHandler();
            services.AddSingleton<ILogHubEventSource>(logHubMessageHandler);
            services.AddSingleton<ILogHubSubscriptions>(logHubMessageHandler);

            services.AddSingleton<FileLogScannerFactory>();

            services.AddSingleton<FileLogWorker>();

            services.AddHostedService(
                serviceProvider =>
                new NgrokWorker(
                    serviceProvider.GetService<IHubContext<LogHub>>()));

            services.AddHostedService(
               serviceProvider =>
               new ProcessWorker(
                   serviceProvider.GetService<IHubContext<LogHub>>()));
        }

        /// <summary>
        /// // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder used to set up the pipeline.</param>
        /// <param name="env">The hosting environment for the server.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
#pragma warning disable CS0618 // Type or member is obsolete
                WebpackDevMiddleware.UseWebpackDevMiddleware(app, new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true,
                    ReactHotModuleReplacement = true,
                });
#pragma warning restore CS0618 // Type or member is obsolete
            }
            else
            {
                app.UseExceptionHandler("/Main/Error");
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Main}/{action=Index}/{id?}");

                endpoints.MapHub<LogHub>("/hub");

                endpoints.MapFallbackToController("Index", "Main");
            });
        }
    }
}
