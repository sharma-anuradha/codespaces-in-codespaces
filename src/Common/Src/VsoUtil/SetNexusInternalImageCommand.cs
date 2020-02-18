// <copyright file="SetNexusInternalImageCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Sets the Nexus internal image version.
    /// </summary>
    [Verb("setnexusinternalimage", HelpText = "Sets the Nexus Internal image version.")]
    public class SetNexusInternalImageCommand : CommandBase
    {
        /// <summary>
        /// The name of the internal windows sku.
        /// </summary>
        private string internalWindowsSkuName = "internalWindows";

        /// <summary>
        /// Gets or sets the nexus image version.
        /// </summary>
        [Option('v', "version", Default = null, HelpText = "Nexus image version. Defaults to version specified in system catelog (appsettings.images.json)")]
        public string ImageVersion { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteAsync(services, stdout).Wait();
        }

        private static string GetNexusInternalConfigurationKey()
        {
            return $"images:compute:nexusInternalWindowsImage:version";
        }

        private async Task ExecuteAsync(IServiceProvider services, TextWriter stdout)
        {
            var systemConfigurationRepository = services.GetRequiredService<ISystemConfigurationRepository>();
            var skuCatalog = services.GetRequiredService<ISkuCatalog>();
            var loggerFactory = services.GetRequiredService<IDiagnosticsLoggerFactory>();
            var logger = loggerFactory.New();

            var internalWindowsSku = skuCatalog.CloudEnvironmentSkus.Values.Where(t => t.SkuName == internalWindowsSkuName).First();

            if (string.IsNullOrEmpty(ImageVersion))
            {
                ImageVersion = internalWindowsSku.ComputeImage.DefaultImageVersion;
            }

            var documentId = GetNexusInternalConfigurationKey();
            var newOverride = new SystemConfigurationRecord
            {
                Id = documentId,
                Value = ImageVersion,
            };

            var currentOverride = await systemConfigurationRepository.GetAsync(documentId, logger);
            if (currentOverride != null)
            {
                // update only if new version is greater than current
                var newVersion = Version.Parse(newOverride.Value);
                var currentVersion = Version.Parse(currentOverride.Value);

                if (newVersion > currentVersion)
                {
                    stdout.Write($"Setting image version to: {newVersion} in region: {Location}...");
                    currentOverride.Value = newOverride.Value;
                    _ = await systemConfigurationRepository.UpdateAsync(currentOverride, logger);
                }
            }
            else
            {
                stdout.Write($"Setting image version to: {newOverride.Value} in region: {Location}...");
                _ = await systemConfigurationRepository.CreateOrUpdateAsync(newOverride, logger);
            }

            stdout.WriteLine("Done.");
        }
    }
}
