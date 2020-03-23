// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.VsSaaS.AspNetCore.Hosting;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Connections;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi
{
    /// <summary>
    /// Configures the ASP.NET Core pipeline for HTTP requests.
    /// </summary>
    public class Startup : CommonStartupBase<AppSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="hostingEnvironment">The hosting environment for the server.</param>
        public Startup(IWebHostEnvironment hostingEnvironment)
            : base(hostingEnvironment, ServiceConstants.ServiceName)
        {
        }

        private PortForwardingHostUtils PortForwardingHostUtils { get; set; } = default!;

        /// <summary>
        /// This method gets called by the runtime.
        /// Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The services collection that should be configured.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers(options =>
                {
                    options.ModelMetadataDetailsProviders.Add(new ExcludeBindingMetadataProvider(typeof(IDiagnosticsLogger)));
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

            // Configuration
            var appSettings = ConfigureAppSettings(services);
            var portForwardingSettings = appSettings.PortForwarding;
            services.AddSingleton(portForwardingSettings);

            // Add front-end/back-end/port-forwarding common services -- secrets, service principal, control-plane resources.
            ConfigureCommonServices(services, out var loggingBaseValues);

            // Adding developer personal stamp settings and resource name builder.
            var developerPersonalStampSettings = new DeveloperPersonalStampSettings(AppSettings.DeveloperPersonalStamp, AppSettings.DeveloperAlias);
            services.AddSingleton(developerPersonalStampSettings);
            services.AddSingleton<IResourceNameBuilder, ResourceNameBuilder>();

            services.AddSingleton<IServiceBusQueueClientProvider, ServiceBusQueueClientProvider>();

            services.AddSingleton<IManagedCache, InMemoryManagedCache>();
            services.AddSingleton<ISystemCatalog, NullSystemCatalog>();

            PortForwardingHostUtils = new PortForwardingHostUtils(portForwardingSettings);
            services.AddSingleton(PortForwardingHostUtils);

            if (IsRunningInAzure() && (
                portForwardingSettings.UseMockKubernetesMappingClientInDevelopment ||
                portForwardingSettings.DisableBackgroundTasksForLocalDevelopment))
            {
                throw new InvalidOperationException("Cannot use mocks, fakes, or disable background tasks outside of local development.");
            }

            if (!portForwardingSettings.DisableBackgroundTasksForLocalDevelopment)
            {
                services.AddEstablishedConnectionsWorker();
            }

            services.AddAgentMappingClient(portForwardingSettings, IsRunningInAzure());

            services.AddVsSaaSHosting(HostingEnvironment, loggingBaseValues);

            // OpenAPI/swagger
            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc(ServiceConstants.CurrentApiVersion, new OpenApiInfo()
                {
                    Title = ServiceConstants.ServiceName,
                    Description = ServiceConstants.ServiceDescription,
                    Version = ServiceConstants.CurrentApiVersion,
                });
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder used to set up the pipeline.</param>
        /// <param name="env">The hosting environment for the server.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            ConfigureAppCommon(app);

            var isProduction = env.IsProduction();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Use VS SaaS middleware.
            app.UseVsSaaS(!isProduction);

            app.MapWhen(Not<HttpContext>(IsUserRequest), HandleInternalRequests);
            app.MapWhen(IsUserRequest, HandleUserRequests);

            Warmup(app);
        }

        private Func<T, bool> Not<T>(Func<T, bool> predicate)
        {
            return (arg) => !predicate(arg);
        }

        private bool IsUserRequest(HttpContext context)
        {
            var isUserHost = PortForwardingHostUtils.IsPortForwardingHost(context.Request.Host.ToString());
            var hasUserHeaders = context.Request.Headers.ContainsKey(PortForwardingHeaders.WorkspaceId)
                && context.Request.Headers.ContainsKey(PortForwardingHeaders.Port);

            return isUserHost || hasUserHeaders;
        }

        private void HandleUserRequests(IApplicationBuilder app)
        {
            app.UseConnectionCreation();
        }

        private void HandleInternalRequests(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(x =>
            {
                x.MapControllers();
            });

            // Swagger/OpenAPI
            app.UseSwagger(x =>
            {
                x.RouteTemplate = "api/{documentName}/swagger";
                x.PreSerializeFilters.Add((swaggerDoc, request) =>
                {
                    var scheme = request.Host.Host == "localhost" ? "http" : "https";
                    var host = request.Host.Value;

                    swaggerDoc.Servers.Add(new OpenApiServer()
                    {
                        Url = $"{scheme}://{host}",
                    });
                });
            });

            app.UseSwaggerUI(c =>
            {
                // UI should be visible from /api/swagger endpoint. Note that leading slash
                // is not present on the RoutePrefix below. This is intentional, because it
                // doesn't work correctly if you add it. I think this is a bug in the Swashbuckle
                // Swagger UI implementation.
                c.RoutePrefix = "api/swagger";

                // The swagger json document is served up from /api/v1/swagger.
                c.SwaggerEndpoint($"/api/{ServiceConstants.CurrentApiVersion}/swagger", ServiceConstants.EndpointName);
                c.DisplayRequestDuration();
            });
        }
    }
}
