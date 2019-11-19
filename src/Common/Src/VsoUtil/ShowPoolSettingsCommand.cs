// <copyright file="ShowPoolSettingsCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// Show a resource pool override.
    /// </summary>
    [Verb("showpoolsettings", HelpText = "Show a resource pool override.")]
    public class ShowPoolSettingsCommand : PoolSettingsCommandBase
    {
        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteCommandAsync(services, stdout, stderr).Wait();
        }

        private async Task ExecuteCommandAsync(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            var settings = await GetResourcePoolSettingsRecordAsync(services);
            if (settings == null)
            {
                throw new InvalidOperationException($"Settings for pool '{Id}' do not exist. Use listpoolsettings to discover existing settings.");
            }

            var json = JsonSerializeObject(settings);
            stdout.WriteLine(json);
        }
    }
}
