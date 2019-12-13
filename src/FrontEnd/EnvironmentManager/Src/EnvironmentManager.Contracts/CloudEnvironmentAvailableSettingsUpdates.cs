// <copyright file="CloudEnvironmentAvailableSettingsUpdates.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// The environment settings which are allowed for an environment.
    /// </summary>
    public class CloudEnvironmentAvailableSettingsUpdates
    {
        /// <summary>
        /// Gets or sets the SKUs which the cloud environment can be updated to.
        /// </summary>
        public ICloudEnvironmentSku[] AllowedSkus { get; set; }

        /// <summary>
        /// Gets or sets the auto-shutdown delays the environment can be updated to.
        /// </summary>
        public int[] AllowedAutoShutdownDelayMinutes { get; set; }
    }
}
