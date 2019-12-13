// <copyright file="CloudEnvironmentAvailableUpdatesResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments
{
    /// <summary>
    /// The environment available settings updates REST API result.
    /// </summary>
    public class CloudEnvironmentAvailableUpdatesResult
    {
        /// <summary>
        /// Gets or sets the SKU names which the cloud environment can be updated to.
        /// </summary>
        public SkuInfoResult[] AllowedSkus { get; set; }

        /// <summary>
        /// Gets or sets the auto-shutdown delays the environment can be updated to.
        /// </summary>
        public int[] AllowedAutoShutdownDelayMinutes { get; set; }
    }
}
