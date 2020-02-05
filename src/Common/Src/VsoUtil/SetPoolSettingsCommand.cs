// <copyright file="SetPoolSettingsCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// Set a resource pool override.
    /// </summary>
    [Verb("setpoolsettings", HelpText = "Set a resource pool override.")]
    public class SetPoolSettingsCommand : PoolSettingsCommandBase
    {
        /// <summary>
        /// Gets or sets the pool id.
        /// </summary>
        [Option('t', "is-enabled", HelpText = "Override the pool enabled state.")]
        public bool? IsEnabled { get; set; } = null;

        /// <summary>
        /// Gets or sets the pool id.
        /// </summary>
        [Option('t', "target-count", HelpText = "The pool target count.")]
        public int? TargetCount { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether to reset unspecified values to null.
        /// </summary>
        [Option('r', "reset", HelpText = "Reset unspecified values to null.")]
        public bool Reset { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteCommandAsync(services, stdout).Wait();
        }

        private async Task ExecuteCommandAsync(IServiceProvider services, TextWriter stdout)
        {
            var snapshot = await GetResourcePoolSnapshotAsync(services);
            if (snapshot is null)
            {
                throw new InvalidOperationException($"Pool with id {Id} does not exist.");
            }

            // Retrieve the original if it exists.
            var resourcePoolSettings = await GetResourcePoolSettingsRecordAsync(services) ?? new ResourcePoolSettingsRecord { Id = Id };

            if (IsEnabled.HasValue)
            {
                resourcePoolSettings.IsEnabled = IsEnabled.Value;
            }
            else if (Reset)
            {
                resourcePoolSettings.IsEnabled = null;
            }

            if (TargetCount.HasValue)
            {
                resourcePoolSettings.TargetCount = TargetCount.Value;
            }
            else if (Reset)
            {
                resourcePoolSettings.TargetCount = null;
            }

            resourcePoolSettings = await CreateOrUpdateResourcePoolSettingsRecordAsync(services, resourcePoolSettings);
            var json = JsonSerializeObject(resourcePoolSettings);
            stdout.WriteLine(json);
        }
    }
}
