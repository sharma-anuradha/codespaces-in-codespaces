// <copyright file="SystemConfigurationCommandBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    public abstract class SystemConfigurationCommandBase : CommandBase
    {
        /// <summary>
        /// Gets a value indicating whether SystemConfigurationCommandBase should use BackEnd.
        /// </summary>
        [Option('b', "backend", Default = false, Required = false, HelpText = "A value indicating whether SystemConfigurationCommandBase should use BackEnd.")]
        public virtual bool UseBackEnd { get; set; }

        /// <summary>
        /// Creates the web host.
        /// </summary>
        /// <param name="webHostArgs">The web host arguments.</param>
        /// <returns>The built web host.</returns>
        protected override IWebHost CreateWebHost(string[] webHostArgs)
        {
            if (UseBackEnd) 
            {
                return base.CreateWebHost(webHostArgs);
            }

            var webHost = WebHost.CreateDefaultBuilder(webHostArgs)
                .UseStartup<StartupFrontEnd>()
                .Build();

            StartupFrontEnd.Services = webHost.Services;

            return webHost;
        }

        protected override void OnWebHostBuilt(IWebHost webHost)
        {
            if (UseBackEnd)
            {
                return;
            }

            var systemConfig = (ISystemConfiguration)webHost.Services.GetService(typeof(ISystemConfiguration));
            var frontEndAppSettings = StartupFrontEnd.FrontEndAppSettings;

            frontEndAppSettings.EnvironmentManagerSettings.Init(systemConfig);
            frontEndAppSettings.PlanManagerSettings.Init(systemConfig);
            frontEndAppSettings.EnvironmentMonitorSettings.Init(systemConfig);
        }

        protected async Task UpdateSystemConfigurationAsync(IServiceProvider services, string id, string value, TextWriter stdout, TextWriter stderr, string comment = default)
        {
            var repository = services.GetRequiredService<ISystemConfigurationRepository>();
            var logger = new NullLogger();

            var record = await repository.GetAsync(id, logger);

            if (record == null && value == null)
            {
                // User is trying to delete a non-existent feature flag.
                await stdout.WriteLineAsync($"System configuration setting for {id} not found, so cannot remove");
                return;
            }

            if (Verbose || DryRun)
            {
                if (record != null)
                {
                    await stdout.WriteLineAsync("Current System Configuration Setting:");
                    await stdout.WriteLineAsync($"  ID: {record.Id}");
                    await stdout.WriteLineAsync($"  Value: {record.Value}");
                    await stdout.WriteLineAsync($"  Comment: {record.Comment}");
                }
                else
                {
                    await stdout.WriteLineAsync($"System configuration setting for {id} not found.");
                }

                await stdout.WriteLineAsync();
            }

            if (value != null)
            {
                record ??= new SystemConfigurationRecord
                {
                    Id = id,
                };

                record.Value = value;
                record.Comment = string.IsNullOrEmpty(comment) ? null : comment;

                await DoWithDryRun(() => repository.CreateOrUpdateAsync(record, logger));

                if (Verbose)
                {
                    await stdout.WriteLineAsync("Updated System Configuration Setting:");
                    await stdout.WriteLineAsync($"  ID: {record.Id}");
                    await stdout.WriteLineAsync($"  Value: {record.Value}");
                    await stdout.WriteLineAsync($"  Comment: {record.Comment}");
                }
            }
            else
            {
                await DoWithDryRun(() => repository.DeleteAsync(id, logger));

                await WriteVerboseLineAsync(stdout, $"System configuration for {id} removed.");
            }
        }
    }
}
