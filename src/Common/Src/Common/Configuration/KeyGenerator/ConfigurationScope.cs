// <copyright file="ConfigurationScope.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator
{
    /// <summary>
    /// Enum to indicate the scope of a configuration key.
    /// </summary>
    public enum ConfigurationScope
    {
        /// <summary>
        /// Configuration scope of Service.
        /// </summary>
        Service = 1,

        /// <summary>
        /// Configuration scope of Region.
        /// </summary>
        Region = 2,

        /// <summary>
        /// Configuration scope of Subscription.
        /// </summary>
        Subscription = 3,

        /// <summary>
        /// Configuration scope of Plan.
        /// </summary>
        Plan = 4,
    }
}