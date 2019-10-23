// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Catalog
{
    /// <summary>
    /// Configures the ASP.NET Core pipeline for HTTP requests.
    /// </summary>
    public partial class Startup : CommonStartupBase<AppSettingsBase>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="hostingEnvironment">The hosting environment for the server.</param>
        public Startup(IHostingEnvironment hostingEnvironment)
            : base(hostingEnvironment, "Catalog")
        {
        }

        /// <summary>
        /// This method gets called by the runtime.
        /// Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The services collection that should be configured.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Configuration AppSettings
            _ = ConfigureAppSettings(services);

            // Add front-end/back-end common services -- secrets, service principal, control-plane resources.
            ConfigureCommonServices(services, out var _);

            services.AddCapacityManager(develperPersonalStamp: false, mocksSettings: null);

            // Add required Mocks
            services.AddDocumentDbClientProvider(options =>
            {
                var (hostUrl, authKey) = ("https://mock-document-db/", "auth-key");
                options.ConnectionMode = Microsoft.Azure.Documents.Client.ConnectionMode.Direct;
                options.ConnectionProtocol = Microsoft.Azure.Documents.Client.Protocol.Tcp;
                options.HostUrl = hostUrl;
                options.AuthKey = authKey;
                options.DatabaseId = "mock-database-id";
                options.PreferredLocation = CurrentAzureLocation.ToString();
                options.UseMultipleWriteLocations = false;
            });

            services.AddSingleton<IHealthProvider, NullHealthProvider>();
            services.AddSingleton<IDiagnosticsLoggerFactory, NullDiagnosticsLoggerFactory>();
            services.AddSingleton(new LogValueSet());
        }

        /// <summary>
        /// // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder used to set up the pipeline.</param>
        /// <param name="env">The hosting environment for the server.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            ConfigureAppCommon(app);
        }

        /// <inheritdoc/>
        protected override string GetSettingsRelativePath()
        {
            // Use current directory.
            return string.Empty;
        }

        private class NullHealthProvider : IHealthProvider
        {
            public bool IsHealthy => true;

            public void MarkUnhealthy(Exception exception, IDiagnosticsLogger logger)
            {
            }
        }
    }
}