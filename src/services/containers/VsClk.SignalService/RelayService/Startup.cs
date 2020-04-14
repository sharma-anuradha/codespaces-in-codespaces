// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VsCloudKernel.SignalService;

namespace Microsoft.VsCloudKernel.RelayService
{
    /// <summary>
    /// Our main Startup class.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Map to the relay hub signalR.
        /// </summary>
        private const string RelayHubMap = "/relayhub";

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Service options
            services.AddSingleton((srvcProvider) => new HubServiceOptions() { Id = Guid.NewGuid().ToString(), Stamp = "local" });
            services.AddSingleton<SignalService.RelayService>();
            services.AddSingleton<IHubContextHost, HubContextHost<RelayServiceHub, RelayServiceHub>>();

            services.AddSignalR()
                .AddNewtonsoftJsonProtocol()
                .AddMessagePackProtocol((options) =>
                {
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(routes =>
            {
                routes.MapHub<RelayServiceHub>(RelayHubMap, options =>
                {
                    options.Transports = AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                });
            });
        }
    }
}