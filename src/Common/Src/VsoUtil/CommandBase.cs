// <copyright file="CommandBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
        /// <summary>
        /// The ASPNETCORE_ENVIRONMENT env var.
        /// </summary>
        public const string EnvironmentNameEnvVarName = "ASPNETCORE_ENVIRONMENT";

        /// <summary>
        /// The AZURE_LOCATION env var.
        /// </summary>
        public const string AzureLocationEnvVarName = "AZURE_LOCATION";

        /// <summary>
        /// The OVERRIDE_APPSETTINGS_JSON env var.
        /// </summary>
        public const string AppSettingsOverrideEnvVarName = "OVERRIDE_APPSETTINGS_JSON";

        /// <summary>
        /// The UseSecretFromAppConfig env var.
        /// </summary>
        public const string UseSecretFromAppConfigEnvVarName = "UseSecretFromAppConfig";

        /// <summary>
        /// The UserAccessToken env var.
        /// </summary>
        public const string UserAccessTokenEnvVarName = "UserAccessToken";

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
        /// Gets or sets a value indicating whether or not to use the SP client secret from user's appsettings.json.
        /// </summary>
        [Option("secret-from-app-config", Required = false, HelpText = "Use SP credentials from personal appsettings.json.")]
        public bool UseSecretFromAppConfig { get; set; }

        /// <summary>
        /// Gets or sets the user's Azure access token.
        /// </summary>
        [Option("token", Required = false, HelpText = "Provides user's token to access Azure resources instead of the SP.")]
        public string UserAccessToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to perform a dry run only.
        /// </summary>
        [Option("dry-run", Default = false, HelpText = "Run as a dry run only (not all commands respect this setting)")]
        public bool DryRun { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use verbose logging.
        /// </summary>
        [Option("verbose", Default = false, HelpText = "Enable verbose logging")]
        public bool Verbose { get; set; }

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

        /// <summary>
        /// Performs the action if <see cref="DryRun"/> is not enable.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <returns>The task of the given action.</returns>
        protected async Task DoWithDryRun(Func<Task> action)
        {
            if (DryRun)
            {
                return;
            }

            await action.Invoke();
        }

        /// <summary>
        /// Output the given message if verbose logging is enabled.
        /// </summary>
        /// <param name="writer">The output writer.</param>
        /// <param name="msg">The message to write.</param>
        /// <returns>The task of the given action.</returns>
        protected async Task WriteVerboseLineAsync(TextWriter writer, string msg)
        {
            if (!Verbose)
            {
                return;
            }

            await writer.WriteLineAsync(msg);
        }

        /// <summary>
        /// Creates the web host.
        /// </summary>
        /// <param name="webHostArgs">THe web host arguments.</param>
        /// <returns>The built web host.</returns>
        protected virtual IWebHost CreateWebHost(string[] webHostArgs)
        {
            var webHost = WebHost.CreateDefaultBuilder(webHostArgs)
                .UseStartup<Startup>()
                .Build();

            Startup.Services = webHost.Services;

            return webHost;
        }

        private IWebHost BuildWebHost()
        {
            System.Environment.SetEnvironmentVariable(EnvironmentNameEnvVarName, Environment, EnvironmentVariableTarget.Process);

            if (!string.IsNullOrEmpty(Override))
            {
                System.Environment.SetEnvironmentVariable(AppSettingsOverrideEnvVarName, Override, EnvironmentVariableTarget.Process);
            }

            if (!string.IsNullOrEmpty(Location))
            {
                System.Environment.SetEnvironmentVariable(AzureLocationEnvVarName, Location, EnvironmentVariableTarget.Process);
            }

            if (UseSecretFromAppConfig)
            {
                System.Environment.SetEnvironmentVariable(UseSecretFromAppConfigEnvVarName, "1", EnvironmentVariableTarget.Process);
            }

            if (!string.IsNullOrEmpty(UserAccessToken))
            {
                System.Environment.SetEnvironmentVariable(UserAccessTokenEnvVarName, UserAccessToken, EnvironmentVariableTarget.Process);
            }

            var webHostArgs = new string[0];
            var webHost = CreateWebHost(webHostArgs);

            // Mini-hack. Ends up that Startup.Configure(IApplicationBuilder) is never called.
            ApplicationServicesProvider.TrySetServiceProvider(webHost.Services);

            return webHost;
        }
    }
}
