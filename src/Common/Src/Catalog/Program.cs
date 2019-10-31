// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using CommandLine;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Catalog
{
    /// <summary>
    /// The catalog command-line untility.
    /// </summary>
    public static class Program
    {
        private static IServiceProvider serviceProvider;

        /// <summary>
        /// The main program entry point.
        /// </summary>
        /// <param name="args">The args list.</param>
        /// <returns>The exit code.</returns>
        public static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<ShowSkusOptions, ShowSubscriptionOptions, ShowControlPlaneOptions>(args)
                .MapResult(
                    (ShowSkusOptions options) => ShowSkus(options),
                    (ShowSubscriptionOptions options) => ShowSubscriptions(options),
                    (ShowControlPlaneOptions options) => ShowControlPlaneInfo(options),
                    errs => 1);
        }

        /// <summary>
        /// Gets the global <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="options">The command line options.</param>
        /// <returns>The service provider instacne.</returns>
        public static IServiceProvider GetServiceProvider(CommonOptions options)
        {
            if (serviceProvider is null)
            {
                IWebHost webHost = BuildWebHost(options);
                serviceProvider = webHost.Services;
            }

            return serviceProvider;
        }
        
        private static int ShowSkus(ShowSkusOptions options)
        {
            try
            {
                var systemCatalog = LoadCatalog(options);
                Commands.WriteSkuCatalog(systemCatalog.SkuCatalog, Console.Out);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static int ShowSubscriptions(ShowSubscriptionOptions options)
        {
            try
            {
                var systemCatalog = LoadCatalog(options);
                Commands.WriteAzureSubscriptionCatalog(systemCatalog.AzureSubscriptionCatalog, GetServiceProvider(options), options.ShowCapacity, Console.Out).Wait();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static int ShowControlPlaneInfo(ShowControlPlaneOptions options)
        {
            try
            {
                var controlPlaneInfo = LoadControlPlaneInfo(options);
                Commands.WriteControlPlaneInfo(controlPlaneInfo, Console.Out);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static ISystemCatalog LoadCatalog(CommonOptions options)
        {
            var services = GetServiceProvider(options);
            var appSettings = (AppSettingsBase)services.GetService(typeof(AppSettingsBase));
            if (string.IsNullOrEmpty(appSettings.ControlPlaneSettings.SubscriptionId))
            {
                appSettings.ControlPlaneSettings.SubscriptionId = Guid.Empty.ToString();
            }

            var systemCatalog = (ISystemCatalog)services.GetService(typeof(ISystemCatalog));
            return systemCatalog;
        }

        private static IControlPlaneInfo LoadControlPlaneInfo(CommonOptions options)
        {
            var services = GetServiceProvider(options);
            var controlPlaneInfo = (IControlPlaneInfo)services.GetService(typeof(IControlPlaneInfo));
            return controlPlaneInfo;
        }

        private static IWebHost BuildWebHost(CommonOptions options)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", options.Environment, EnvironmentVariableTarget.Process);

            if (!string.IsNullOrEmpty(options.Override))
            {
                Environment.SetEnvironmentVariable("OVERRIDE_APPSETTINGS_JSON", options.Override, EnvironmentVariableTarget.Process);
            }

            if (!string.IsNullOrEmpty(options.Location))
            {
                Environment.SetEnvironmentVariable("AZURE_LOCATION", options.Location, EnvironmentVariableTarget.Process);
            }

            var webHostArgs = new string[0];
            var webHost = WebHost.CreateDefaultBuilder(webHostArgs)
                .UseStartup<Startup>()
                .Build();
            return webHost;
        }

        /// <summary>
        /// Common options.
        /// </summary>
        public class CommonOptions
        {
            /// <summary>
            /// Gets or sets the ASPNETCORE_ENVIRONMENT.
            /// </summary>
            [Option('e', "env", Default = "Production", HelpText = "The ASPNETCORE_ENVIRONMENT name. Valid values are Production, Staging, Development.")]
            public string Environment { get; set; }

            /// <summary>
            /// Gets or sets the OVERRIDE_APPSETTINGS_JSON.
            /// </summary>
            [Option('o', "override", Required = false, HelpText = "The OVERRIDE_APPSETTINGS_JSON name.")]
            public string Override { get; set; }

            /// <summary>
            /// Gets or sets the Azure Location.
            /// </summary>
            [Option('l', "location", Required = false, HelpText = "The control-plane Azure Location. Default is West US 2.")]
            public string Location { get; set; }
        }

        /// <summary>
        /// The show skus verb.
        /// </summary>
        [Verb("skus", HelpText = "Show the SKU catalog.")]
        public class ShowSkusOptions : CommonOptions
        {
        }

        /// <summary>
        /// The show subscriptions verb.
        /// </summary>
        [Verb("subscriptions", HelpText = "Show the subscription catalog.")]
        public class ShowSubscriptionOptions : CommonOptions
        {
            /// <summary>
            /// Gets or sets a value indicating whether to include the subscription capacity.
            /// </summary>
            [Option('c', "show-capacity", HelpText = "Show the subscription capacity.")]
            public bool ShowCapacity { get; set; }
        }

        /// <summary>
        /// The show control-plan info verb.
        /// </summary>
        [Verb("controlplane", HelpText = "Show the control plane info.")]
        public class ShowControlPlaneOptions : CommonOptions
        {
        }
    }
}
