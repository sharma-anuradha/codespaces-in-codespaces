// <copyright file="ConfigurationKeyGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator
{
    /// <summary>
    /// A key generator for various configurations.
    /// </summary>
    public class ConfigurationKeyGenerator : IConfigurationKeyGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationKeyGenerator"/> class.
        /// </summary>
        /// <param name="options">The azure resource provider options.</param>
        /// <param name="controlPlaneInfo">control plane info.</param>
        public ConfigurationKeyGenerator(IOptions<ControlPlaneInfoOptions> options, IControlPlaneInfo controlPlaneInfo)
        {
            ControlPlaneSettings = Requires.NotNull(options?.Value?.ControlPlaneSettings, nameof(ControlPlaneSettings));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
        }

        private ControlPlaneSettings ControlPlaneSettings { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private string ServiceScopeConfiguration =>
            $"{ControlPlaneSettings.Prefix}";

        private string RegionScopeConfiguration =>
            $"{ServiceScopeConfiguration}-{ControlPlaneStampInfo.RegionCodes[ControlPlaneInfo.Stamp.Location]}";

        private string SubscriptionScopeConfiguration =>
            $"{RegionScopeConfiguration}-subscription";

        /// <inheritdoc/>
        public string GenerateServiceScopeConfigurationKey(ConfigurationType configurationType, string componentName, string configurationName)
        {
            return GenerateConfigurationKey(configurationType, componentName, configurationName, ConfigurationScope.Service);
        }

        /// <inheritdoc/>
        public string GenerateRegionScopeConfigurationKey(ConfigurationType configurationType, string componentName, string configurationName)
        {
            return GenerateConfigurationKey(configurationType, componentName, configurationName, ConfigurationScope.Region);
        }

        /// <inheritdoc/>
        public string GenerateSubscriptionScopeConfigurationKey(ConfigurationType configurationType, string componentName, string configurationName, string subscriptionId)
        {
            return GenerateConfigurationKey(configurationType, componentName, configurationName, ConfigurationScope.Subscription, subscriptionId);
        }

        /// <inheritdoc/>
        public string GeneratePlanScopeConfigurationKey(ConfigurationType configurationType, string componentName, string configurationName, string subscriptionId, string planName)
        {
            return GenerateConfigurationKey(configurationType, componentName, configurationName, ConfigurationScope.Plan, subscriptionId, planName);
        }

        private string GenerateConfigurationKey(ConfigurationType configurationType, string componentName, string configurationName, ConfigurationScope configurationScope, string subscriptionId = default, string planName = default)
        {
            string configType = configurationType.ToString();
            string scope = GetScopeString(configurationScope, subscriptionId, planName);
            string keyName = $"{componentName}-{configurationName}";
            string key = $"{configType}:{scope}:{keyName}";
            return key.ToLower();
        }

        private string GetScopeString(ConfigurationScope configurationScope, string subscriptionId, string planName)
        {
            return configurationScope switch
            {
                ConfigurationScope.Service => ServiceScopeConfiguration,
                ConfigurationScope.Region => RegionScopeConfiguration,
                ConfigurationScope.Subscription => $"{SubscriptionScopeConfiguration}-{subscriptionId}",
                ConfigurationScope.Plan => $"{SubscriptionScopeConfiguration}-{subscriptionId}-plan-{planName}",
                _ => throw new ArgumentException(message: "invalid enum value", paramName: nameof(configurationScope)),
            };
        }
    }
}