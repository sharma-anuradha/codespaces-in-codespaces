// <copyright file="CurrentImageInfoProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration
{
    /// <inheritdoc/>
    public class CurrentImageInfoProvider : ICurrentImageInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentImageInfoProvider"/> class.
        /// </summary>
        /// <param name="systemConfiguration">The system configuration to use for querying current values.</param>
        public CurrentImageInfoProvider(ISystemConfiguration systemConfiguration)
        {
            Requires.NotNull(systemConfiguration, nameof(systemConfiguration));
            SystemConfiguration = systemConfiguration;
        }

        private ISystemConfiguration SystemConfiguration { get; }

        /// <inheritdoc/>
        public Task<string> GetImageNameAsync(
            ImageFamilyType imageFamilyType,
            string imageFamilyName,
            string defaultImageName,
            IDiagnosticsLogger logger)
        {
            var key = GetConfigurationKey(imageFamilyType, imageFamilyName, "name");
            return SystemConfiguration.GetValueAsync(key, logger, defaultImageName);
        }

        /// <inheritdoc/>
        public Task<string> GetImageVersionAsync(
            ImageFamilyType imageFamilyType,
            string imageFamilyName,
            string defaultImageVersion,
            IDiagnosticsLogger logger)
        {
            var key = GetConfigurationKey(imageFamilyType, imageFamilyName, "version");
            return SystemConfiguration.GetValueAsync(key, logger, defaultImageVersion);
        }

        /// <summary>
        /// Generates the key to use to look up in the configuration database.
        /// </summary>
        /// <param name="imageFamilyType">The image family type.</param>
        /// <param name="imageFamilyName">The image family name.</param>
        /// <param name="valueType">The value to lookup for the image family.</param>
        /// <returns>The lookup key.</returns>
        /// <example>"images:vmagent:vsoagentlinux:name".</example>
        /// <example>"images:vmagent:vsoagentlinux:version".</example>
        /// <example>"images:compute:nexusInternalWindowsImage:version".</example>
        /// <remarks>Look at appsettings.images.json for reference about which values can be queried.</remarks>
        private static string GetConfigurationKey(ImageFamilyType imageFamilyType, string imageFamilyName, string valueType)
        {
            return $"images:{imageFamilyType.ToString().ToLowerInvariant()}:{imageFamilyName}:{valueType}";
        }
    }
}
