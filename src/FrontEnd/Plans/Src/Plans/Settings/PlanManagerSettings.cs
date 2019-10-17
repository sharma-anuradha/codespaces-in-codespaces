// <copyright file="PlanManagerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    public class PlanManagerSettings
    {
        /// <summary>
        /// Gets or sets the SkuPlan Quota.
        /// </summary>
        public int MaxPlansPerSubscription { get; set; }
    }
}
