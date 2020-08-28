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
        /// </summary>
        Feature = 1,

        /// <summary>
        ///  Configuration type for quota.
        /// </summary>
        Quota = 2,

        /// <summary>
        /// Configuration type for setting.
        /// </summary>
        Setting = 3,
    }
}