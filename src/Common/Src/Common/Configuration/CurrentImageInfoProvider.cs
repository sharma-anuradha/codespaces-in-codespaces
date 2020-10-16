// <copyright file="CurrentImageInfoProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration
{
    /// <inheritdoc/>
    public class CurrentImageInfoProvider : ICurrentImageInfoProvider
    {
        private const string LogBaseName = "current_image_info_provider";

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentImageInfoProvider"/> class.
        /// </summary>
        /// <param name="configurationReader">Configuration reader - to use for querying current values.</param>
        public CurrentImageInfoProvider(IConfigurationReader configurationReader)
        {
            Requires.NotNull(configurationReader, nameof(configurationReader));
            ConfigurationReader = configurationReader;
        }

        private IConfigurationReader ConfigurationReader { get; }

        /// <inheritdoc/>
        public Task<string> GetImageNameAsync(
            ImageFamilyType imageFamilyType,
            string imageFamilyName,
            string defaultImageName,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_get_image_name",
                async (childLogger) =>
                {
                    // example of keys - setting:vsclk:images-vmagent-vsoagentlinux-name (global scope, least priority), setting:vsclk-usw2:images-vmagent-vsoagentlinux-name (region scope, highest priority)
                    // setting:vsclk:images-compute-nexusInternalWindowsImage-name (global scope, least priority), setting:vsclk-usw2:images-compute-nexusInternalWindowsImage-name (region scope, highest priority)
                    var (componentName, configurationName) = GetComponentAndConfigurationName(imageFamilyType, imageFamilyName, "name");
                    return await ConfigurationReader.ReadSettingAsync(componentName, configurationName, logger.NewChildLogger(), defaultImageName);
                });
        }

        /// <inheritdoc/>
        public Task<string> GetImageVersionAsync(
            ImageFamilyType imageFamilyType,
            string imageFamilyName,
            string defaultImageVersion,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_get_image_version",
                async (childLogger) =>
                {
                    // example of keys - setting:vsclk:images-vmagent-vsoagentlinux-version (global scope, least priority), setting:vsclk-usw2:images-vmagent-vsoagentlinux-version (region scope, highest priority)
                    // setting:vsclk:images-compute-nexusInternalWindowsImage-version (global scope, least priority), setting:vsclk-usw2:images-compute-nexusInternalWindowsImage-version (region scope, highest priority)
                    var (componentName, configurationName) = GetComponentAndConfigurationName(imageFamilyType, imageFamilyName, "version");
                    return await ConfigurationReader.ReadSettingAsync(componentName, configurationName, logger.NewChildLogger(), defaultImageVersion);
                });
        }

        /// <summary>
        /// Generates the component name and configuration name to use to look up in the configuration database.
        /// </summary>
        /// <param name="imageFamilyType">The image family type.</param>
        /// <param name="imageFamilyName">The image family name.</param>
        /// <param name="valueType">The value to lookup for the image family.</param>
        /// <returns>The component name and configuration name to use for lookup.</returns>
        /// <example>"(images-vmagent, vsoagentlinux-name)".</example>
        /// <example>"(images-vmagent, vsoagentlinux-version)".</example>
        /// <example>"(images-compute, nexusInternalWindowsImage-version)".</example>
        /// <remarks>Look at appsettings.images.json for reference about which values can be queried.</remarks>
        private static (string, string) GetComponentAndConfigurationName(ImageFamilyType imageFamilyType, string imageFamilyName, string valueType)
        {
            var componentName = $"images-{imageFamilyType.ToString().ToLowerInvariant()}";
            var configurationName = $"{imageFamilyName}-{valueType}";
            return (componentName, configurationName);
        }
    }
}
