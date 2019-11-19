// <copyright file="ListPoolsCommand.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// The show pools from CosmosDB.
    /// </summary>
    [Verb("listpools", HelpText = "List the resource pools.")]
    public class ListPoolsCommand : CommandBase
    {
        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteCommandAsync(services, stdout).Wait();
        }

        private async Task ExecuteCommandAsync(IServiceProvider services, TextWriter stdout)
        {
            var repo = services.GetRequiredService<IResourcePoolStateSnapshotRepository>();
            var loggerFactory = services.GetRequiredService<IDiagnosticsLoggerFactory>();
            var logger = loggerFactory.New();
            var settings = (await repo.GetWhereAsync(x => true, logger))
                .ToDictionary(x => x.Id);

            var sorted = new SortedDictionary<string, ResourcePoolStateSnapshotRecord>(settings, StringComparer.OrdinalIgnoreCase);
            var json = JsonSerializeObject(sorted);
            stdout.WriteLine(json);
        }
    }
}
