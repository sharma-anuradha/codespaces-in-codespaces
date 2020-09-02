// <copyright file="ConfigurationType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator
{
    /// <summary>
    /// Defines types of configuration the key generator can work with.
    /// </summary>
    public enum ConfigurationType
    {
        /// <summary>
        /// Configuration type for feature.
        /// A feature is something that is meant to be temporary and will eventually be enabled by default.
        /// Example would be - vnet-injection or queue resource allocation. These will be feature.
        /// </summary>
        Feature = 1,

        /// <summary>
        ///  Configuration type for quota.
        /// </summary>
        Quota = 2,

        /// <summary>
        /// Configuration type for setting.
        /// A setting is something that is meant to change behaviour of service at runtime.
        /// Example would be enabling or disabling background jobs or enabling/disabling subscriptions
        /// </summary>
        Setting = 3,
    }
}