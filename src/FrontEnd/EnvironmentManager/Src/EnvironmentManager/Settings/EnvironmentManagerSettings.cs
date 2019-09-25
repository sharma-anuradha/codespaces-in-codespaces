// <copyright file="EnvironmentManagerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    public class EnvironmentManagerSettings
    {
        /// <summary>
        /// Gets or sets the Cloud Environment Quota.
        /// </summary>
        public int PerUserEnvironmentQuota { get; set; }
    }
}
