// <copyright file="ListPoolSettingsCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// List the resource pool settings.
    /// </summary>
    [Verb("listpoolsettings", HelpText = "List the resource pool settings.")]
    public class ListPoolSettingsCommand : CommandBase
    {
        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteCommandAsync(services, stdout).Wait();
        }

        private async Task ExecuteCommandAsync(IServiceProvider services, TextWriter stdout)
        {
            var repo = services.GetRequiredService<IResourcePoolSettingsRepository>();
            var loggerFactory = services.GetRequiredService<IDiagnosticsLoggerFactory>();
            var logger = loggerFactory.New();
            var settings = (await repo.GetWhereAsync(x => true, logger))
                .ToDictionary(x => x.Id);

            var sorted = new SortedDictionary<string, ResourcePoolSettingsRecord>(settings, StringComparer.OrdinalIgnoreCase);
            var json = JsonSerializeObject(sorted);
            stdout.WriteLine(json);
        }
    }
}
