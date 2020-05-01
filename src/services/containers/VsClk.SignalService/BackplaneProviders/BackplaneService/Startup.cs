// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.Services.Backplane.Common;
using Microsoft.VsCloudKernel.SignalService;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    public class Startup : SignalService.StartupBase<AppSettings>
    {
        public Startup(
            IConfiguration configuration,
            IWebHostEnvironment hostEnvironment,
            ILoggerFactory loggerFactory)
            : base(configuration, hostEnvironment, loggerFactory)
        {
        }

        public override string ServiceType => "Backplane";

        protected override Type AppType => typeof(Startup);

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureCommonServices(services);
            services.AddSingleton<IBackplaneServiceDataProvider, MemoryBackplaneDataProvider>();

            // Create the Azure Cosmos backplane provider service
            services.AddSingleton<IAzureDocumentsProviderServiceFactory, AzureDocumentsProviderFactory>();
            services.AddHostedService<AzureDocumentsProviderService>();

            // Create the Azure Redis backplane provider service
            services.AddSingleton<IAzureRedisProviderServiceFactory, AzureRedisContactsProviderFactory>();
            services.AddSingleton<IAzureRedisProviderServiceFactory, AzureRedisRelayProviderFactory>();
            services.AddHostedService<AzureRedisProviderService>();

            // backplane services
            services.AddSingleton<ContactBackplaneService>();
            services.AddSingleton<RelayBackplaneService>();

            // Our json Rpc Server for contact/relay services
            services.AddSingleton<IJsonRpcSessionFactory, JsonRpcContactSessionFactory>();
            services.AddSingleton<IJsonRpcSessionFactory, JsonRpcRelaySessionFactory>();

            services.AddHostedService<JsonRpcServerService>();

            // Host for ContactBackplaneService
            services.AddHostedService<ApplicationHostedService<ContactBackplaneService>>();

            // Host for RelayBackplaneService
            services.AddHostedService<ApplicationHostedService<RelayBackplaneService>>();

            // Our json Rpc Session Manager
            services.AddSingleton<JsonRpcContactSessionFactory>();
            services.AddSingleton<JsonRpcRelaySessionFactory>();

            // backplane manager support
            services.AddSingleton<IContactBackplaneManager, ContactBackplaneManager>();
            services.AddHostedService<BackplaneManagerHostedService<ContactBackplaneService, IContactBackplaneManager>>();
            services.AddSingleton<IRelayBackplaneManager, RelayBackplaneManager>();
            services.AddHostedService<BackplaneManagerHostedService<RelayBackplaneService, IRelayBackplaneManager>>();

            // Mvc support
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // configure SignalR service
            app.UseRouting();
            app.UseEndpoints(routes =>
            {
                routes.MapControllers();
            });
        }
    }
}