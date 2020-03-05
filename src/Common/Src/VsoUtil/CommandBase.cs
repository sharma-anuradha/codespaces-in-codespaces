// <copyright file="CommandBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using CommandLine;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// Common options.
    /// </summary>
    public abstract class CommandBase
    {
        private IServiceProvider serviceProvider;

        /// <summary>
        /// Gets or sets the ASPNETCORE_ENVIRONMENT.
        /// </summary>
        [Option('e', "env", Default = "Development", HelpText = "The ASPNETCORE_ENVIRONMENT name. Valid values are Production, Staging, Development.")]
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the Azure Location.
        /// </summary>
        [Option('l', "location", Default = "WestUs2", Required = false, HelpText = "The control-plane Azure Location.")]
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the OVERRIDE_APPSETTINGS_JSON.
        /// </summary>
        [Option('o', "override", Required = false, HelpText = "The OVERRIDE_APPSETTINGS_JSON name.")]
        public string Override { get; set; }

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="stdout">Output text to stdout.</param>
        /// <param name="stderr">Output text to stderr.</param>
        /// <returns>Process return code.</returns>
        public int Execute(TextWriter stdout, TextWriter stderr)
        {
            var services = GetServiceProvider();
            ExecuteCommand(services, stdout, stderr);
            return 0;
        }

        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="services">The service provider.</param>
        /// <param name="stdout">Output text to stdout.</param>
        /// <param name="stderr">Output text to stderr.</param>
        protected abstract void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr);

        /// <summary>
        /// Gets the system catalog.
        /// </summary>
        /// <returns>system catalog.</returns>
        protected ISystemCatalog GetSystemCatalog()
        {
            return GetServiceProvider().GetRequiredService<ISystemCatalog>();
        }

        /// <summary>
        /// Gets the control plane info.
        /// </summary>
        /// <returns>control plane info.</returns>
        protected IControlPlaneInfo GetControlPlaneInfo()
        {
            return GetServiceProvider().GetRequiredService<IControlPlaneInfo>();
        }

        /// <summary>
        /// Gets the service provider.
        /// </summary>
        /// <returns>obj.</returns>
        protected IServiceProvider GetServiceProvider()
        {
            if (serviceProvider is null)
            {
                var webHost = BuildWebHost();
                serviceProvider = webHost.Services;
            }

            return serviceProvider;
        }

        /// <summary>
        /// Serialize an object.
        /// </summary>
        /// <param name="obj">obj.</param>
        /// <returns>json.</returns>
        protected string JsonSerializeObject(object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, new StringEnumConverter());
        }

        private IWebHost BuildWebHost()
        {
            System.Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", Environment, EnvironmentVariableTarget.Process);

            if (!string.IsNullOrEmpty(Override))
            {
                System.Environment.SetEnvironmentVariable("OVERRIDE_APPSETTINGS_JSON", Override, EnvironmentVariableTarget.Process);
            }

            if (!string.IsNullOrEmpty(Location))
            {
                System.Environment.SetEnvironmentVariable("AZURE_LOCATION", Location, EnvironmentVariableTarget.Process);
            }

            var webHostArgs = new string[0];
            var webHost = WebHost.CreateDefaultBuilder(webHostArgs)
                .UseStartup<Startup>()
                .Build();

            // Mini-hack. Ends up that Startup.Configure(IApplicationBuilder) is never called.
            Startup.Services = webHost.Services;
            ApplicationServicesProvider.TrySetServiceProvider(webHost.Services);

            return webHost;
        }
    }
}
