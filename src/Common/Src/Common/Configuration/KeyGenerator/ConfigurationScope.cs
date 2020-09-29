// <copyright file="ConfigurationScope.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator
{
    /// <summary>
    /// Enum to indicate the scope of a configuration key.
    /// They are defined in the increasing order of priority i.e. Service scope has the
    /// least priority whereas the user scope has the highest priority. So keep this enum
    /// in sorted order of priority.
    /// </summary>
    public enum ConfigurationScope
    {
        /// <summary>
        /// Configuration scope of Service.
        /// </summary>
        Service,

        /// <summary>
        /// Configuration scope of Region.
        /// </summary>
        Region,

        /// <summary>
        /// Configuration scope of Subscription.
        /// </summary>
        Subscription,

        /// <summary>
        /// Configuration scope of Plan.
        /// </summary>
        Plan,

        /// <summary>
        /// Configuration scope of User.
        /// </summary>
        User,
    }
}