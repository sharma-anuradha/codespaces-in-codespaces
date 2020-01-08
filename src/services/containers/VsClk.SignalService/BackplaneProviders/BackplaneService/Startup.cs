using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    public class Startup : SignalService.StartupBase<AppSettings>
    {
        private const string BackplaneHubMap = "/backplanehub";

        protected override string ServiceType => "Backplane";

        protected override Type AppType => typeof(Startup);

        public Startup(ILoggerFactory loggerFactory, IWebHostEnvironment env)
            : base(loggerFactory, env)
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureCommonServices(services);
            services.AddSingleton<IBackplaneServiceDataProvider, MemoryBackplaneDataProvider>();

            // Create the Azure Cosmos backplane provider service
            services.AddHostedService<AzureDocumentsProviderService>();

            // Create the Azure Redis backplane provider service
            services.AddHostedService<AzureRedisProviderService>();

            // Hub IContactBackplaneServiceNotification instance
            services.AddSingleton<IContactBackplaneServiceNotification, HubContactBackplaneServiceNotification>();

            // SignalR/jsonRpc support services
            services.AddSingleton<ContactBackplaneService>();

            // Our json Rpc Server
            services.AddHostedService<JsonRpcServerService>();

            // Host for ContactBackplaneService
            services.AddHostedService<ContactBackplaneHostedService>();

            // Our json Rpc Session Manager
            services.AddSingleton<JsonRpcSessionManager>();

            // backplane manager support
            services.AddSingleton<IContactBackplaneManager, ContactBackplaneManager>();
            services.AddHostedService<ContactBackplaneManagerHostedService<ContactBackplaneService>>();

            // Mvc support
            services.AddControllers();

            // signalR support
            services.AddSignalR(hubOptions =>
            {
                hubOptions.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
                hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(1);
            });
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
                routes.MapHub<ContactBackplaneHub>(BackplaneHubMap);
                routes.MapControllers();
            });
        }
    }
}