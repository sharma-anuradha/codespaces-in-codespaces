// <copyright file="SetSkuImageVersionCommand.cs" company="Microsoft">
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
    /// Sets the specified SKU's image version.
    /// </summary>
    [Verb("setskuimageversion", HelpText = "Sets the specified SKU's image version. Defaults are set for updating Nexus windowsInternal SKU.")]
    public class SetSkuImageVersionCommand : CommandBase
    {
        /// <summary>
        /// Gets or sets the name of the sku whose image version to update.
        /// </summary>
        [Option('s', "sku", Default = "internalWindows", HelpText = "Sku name. Defaults to internalWindows Sku.")]
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the image version.
        /// </summary>
        [Option('v', "version", Default = null, HelpText = "Image version. Defaults to version specified in system catelog (appsettings.images.json)")]
        public string ImageVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the image version can be reverted to an earlier version.
        /// </summary>
        [Option('f', "force", Default = false, HelpText = "Force option. Defaults to false which restricts the update of image version to only newer versions.")]
        public bool Force { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteAsync(services, stdout).Wait();
        }

        private string GetConfigurationKey(string imageName)
        {
            return $"images:compute:{imageName}:version";
        }

        private async Task ExecuteAsync(IServiceProvider services, TextWriter stdout)
        {
            var systemConfigurationRepository = services.GetRequiredService<ISystemConfigurationRepository>();
            var skuCatalog = services.GetRequiredService<ISkuCatalog>();
            var loggerFactory = services.GetRequiredService<IDiagnosticsLoggerFactory>();
            var logger = loggerFactory.New();

            var sku = skuCatalog.CloudEnvironmentSkus.Values.Where(t => t.SkuName == SkuName).First();
            var imageFamilyName = sku.ComputeImage.ImageFamilyName;

            if (string.IsNullOrEmpty(ImageVersion))
            {
                ImageVersion = sku.ComputeImage.DefaultImageVersion;
            }

            var documentId = GetConfigurationKey(imageFamilyName);
            var newOverride = new SystemConfigurationRecord
            {
                Id = documentId,
                Value = ImageVersion,
            };

            var currentOverride = await systemConfigurationRepository.GetAsync(documentId, logger);
            if (currentOverride != null)
            {
                // update only if new version is greater than current
                // force option will allow reverting to a lesser version
                var newVersion = Version.Parse(newOverride.Value);
                var currentVersion = Version.Parse(currentOverride.Value);

                if (newVersion > currentVersion || Force)
                {
                    stdout.Write($"Setting SKU {SkuName} image version to: {newVersion} in region: {Location}...");
                    currentOverride.Value = newOverride.Value;
                    _ = await systemConfigurationRepository.UpdateAsync(currentOverride, logger);
                }
                else
                {
                    stdout.Write($"No changes made. Current:{currentVersion} is newer than requested version:{newVersion} in region: {Location}...");
                }
            }
            else
            {
                stdout.Write($"Setting SKU {SkuName} image version to: {newOverride.Value} in region: {Location}...");
                _ = await systemConfigurationRepository.CreateOrUpdateAsync(newOverride, logger);
            }

            stdout.WriteLine("Done.");
        }
    }
}
