// <copyright file="DeletePoolSettingsCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// Delete a resource pool override.
    /// </summary>
    [Verb("deletepoolsettings", HelpText = "Delete a resource pool override.")]
    public class DeletePoolSettingsCommand : PoolSettingsCommandBase
    {
        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteCommandAsync(services, stdout, stderr).Wait();
        }

        private async Task ExecuteCommandAsync(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            var deleted = await DeleteResourcePoolSettingsRecordAsync(services);
            var json = JsonSerializeObject(deleted);
            stdout.WriteLine(json);
        }
    }
}
