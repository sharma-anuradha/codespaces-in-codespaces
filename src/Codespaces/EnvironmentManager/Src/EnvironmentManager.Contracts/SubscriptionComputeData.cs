// <copyright file="SubscriptionComputeData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Class representing subscription level compute data.
    /// </summary>
    public class SubscriptionComputeData
    {
        /// <summary>
        /// Gets or sets a value indicating whether the subscription has reached
        /// its compute quota or not.
        /// </summary>
        public bool HasReachedQuota { get; set; }

        /// <summary>
        /// Gets or sets the compute quota.
        /// </summary>
        public int ComputeQuota { get; set; }

        /// <summary>
        /// Gets or sets the compute usage.
        /// </summary>
        public int ComputeUsage { get; set; }
    }
}
