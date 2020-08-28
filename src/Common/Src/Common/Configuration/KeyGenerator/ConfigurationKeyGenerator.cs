// <copyright file="ConfigurationKeyGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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

        private string PlanScopeConfiguration =>
            $"{SubscriptionScopeConfiguration}-plan";

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
            var key = GenerateConfigurationKey(configurationType, componentName, configurationName, ConfigurationScope.Subscription);
            return $"{key}-{subscriptionId}".ToLower();
        }

        /// <inheritdoc/>
        public string GeneratePlanScopeConfigurationKey(ConfigurationType configurationType, string componentName, string configurationName, string subscriptionId, string planName)
        {
            var key = GenerateConfigurationKey(configurationType, componentName, configurationName, ConfigurationScope.Plan);
            return $"{key}-{subscriptionId}-{planName}".ToLower();
        }

        private string GenerateConfigurationKey(ConfigurationType configurationType, string componentName, string configurationName, ConfigurationScope configurationScope)
        {
            string configType = configurationType.ToString();
            string scope = GetScopeString(configurationScope);
            string keyName = $"{componentName}{configurationName}";
            string key = $"{configType}:{scope}:{keyName}";
            return key.ToLower();
        }

        private string GetScopeString(ConfigurationScope configurationScope)
        {
            string scope = default;
            switch (configurationScope)
            {
                case ConfigurationScope.Service:
                    scope = ServiceScopeConfiguration;
                    break;
                case ConfigurationScope.Region:
                    scope = RegionScopeConfiguration;
                    break;
                case ConfigurationScope.Subscription:
                    scope = SubscriptionScopeConfiguration;
                    break;
                case ConfigurationScope.Plan:
                    scope = PlanScopeConfiguration;
                    break;
                default:
                    break;
            }

            return scope;
        }
    }
}