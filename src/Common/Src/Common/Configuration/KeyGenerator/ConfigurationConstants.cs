// <copyright file="ConfigurationConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
{
    /// <summary>
    /// Configuration constants.
    /// </summary>
    public static class ConfigurationConstants
    {
        /// <summary>
        /// Name of enabled setting.
        /// </summary>
        public const string EnabledSettingName = "enabled";

        /// <summary>
        /// Name of enabled feature.
        /// </summary>
        public const string EnabledFeatureName = "enabled";

        /// <summary>
        /// String representing plan manager as the component. Mostly aplicable for quota keys/configurations.
        /// </summary>
        public const string PlanManagerComponent = "planmanager";

        /// <summary>
        /// String representing environment manager as the component. Mostly aplicable for quota keys/configurations.
        /// </summary>
        public const string EnvironmentManagerComponent = "environmentmanager";
    }
}
