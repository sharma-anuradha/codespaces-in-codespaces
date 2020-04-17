// <copyright file="SubscriptionManagerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Settings
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    public class SubscriptionManagerSettings
    {
        /// <summary>
        /// Gets or sets how long ago to process recently banned subscriptions.
        /// </summary>
        public int? BannedDaysAgo { get; set; }
    }
}
