// <copyright file="CloudEnvironmentUpdate.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Cloud environment settings to update an existing environment to.
    /// </summary>
    public class CloudEnvironmentUpdate
    {
        /// <summary>
        /// Gets or sets the cloud environment sku name.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the auto shutdown time the user specified.
        /// </summary>
        public int? AutoShutdownDelayMinutes { get; set; }
    }
}
