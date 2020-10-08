// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.AspNetCore.Hosting;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.ErrorsBackend.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.ErrorsBackend
{
    /// <inheritdoc />
    public class Startup : CommonStartupBase<AppSettings>
    {
        private const string AppSecretsSectionName = "AppSecrets";

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="env">The aspnet core WebHost environment.</param>
        public Startup(IWebHostEnvironment env)
            : base(env, ServiceConstants.ServiceName)
        {
        }

        /// <summary>
        /// This method gets called by the runtime.
        /// Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The services collection that should be configured.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureAppSettings(services);

            services.AddLocalization();

            var builder = services
                .AddControllersWithViews()
                .AddViewLocalization();
#if DEBUG
            if (HostingEnvironment.IsDevelopment())
            {
                builder.AddRazorRuntimeCompilation();
            }
#endif

            var productInfo = new ProductInfoHeaderValue(
                ServiceName, Assembly.GetExecutingAssembly().GetName().Version?.ToString());
            services.AddSingleton(productInfo);

            ConfigureAppSecrets(services, Configuration.GetSection(AppSecretsSectionName));

            services.AddApplicationServicePrincipal(AppSettings.ApplicationServicePrincipal);
            services.AddCurrentLocationProvider(CurrentAzureLocation);
            services.AddControlPlaneInfo(AppSettings.ControlPlaneSettings);
            services.AddControlPlaneAzureResourceAccessor();

            // Adding developer personal stamp settings and resource name builder.
            var developerPersonalStampSettings = new DeveloperPersonalStampSettings(AppSettings.DeveloperPersonalStamp, AppSettings.DeveloperAlias, AppSettings.DeveloperKusto);
            services.AddSingleton(developerPersonalStampSettings);
            services.AddSingleton<IResourceNameBuilder, ResourceNameBuilder>();

            services.TryAddSingleton<IManagedCache, InMemoryManagedCache>();

            var loggingBaseValues = new LoggingBaseValues
            {
                ServiceName = ServiceName,
                CommitId = AppSettings.GitCommit,
                AdditionalValues = new Dictionary<string, string>
                {
                    { "BuildId", AppSettings.BuildId },
                    { "BuildNumber", AppSettings.BuildNumber },
                },
            };

            services.AddTransient(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetService<IDiagnosticsLoggerFactory>();
                var logValueSet = serviceProvider.GetService<LogValueSet>();
                return loggerFactory.New(logValueSet);
            });

            services.AddVsSaaSHosting(HostingEnvironment, loggingBaseValues);
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder used to set up the pipeline.</param>
        /// <param name="env">The hosting environment for the server.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseVsSaaS(!HostingEnvironment.IsProduction());

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapFallbackToController("Index", "Default");
            });
        }

        /// <inheritdoc />
        protected override string GetSettingsRelativePath()
        {
            var dirChar = Path.DirectorySeparatorChar.ToString();
            var settingsRelativePath = IsRunningInAzure() ? string.Empty : Path.GetFullPath(
                Path.Combine(HostingEnvironment.ContentRootPath, "..", "..", $"Settings{dirChar}"));
            return settingsRelativePath;
        }
    }
}
