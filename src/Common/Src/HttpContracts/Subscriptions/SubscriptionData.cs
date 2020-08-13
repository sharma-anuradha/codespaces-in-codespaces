// <copyright file="SubscriptionData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Subscriptions
{
    /// <summary>
    /// Class representing Subscription level information.
    /// </summary>
    public class SubscriptionData
    {
        /// <summary>
        /// Gets or sets the Azure subscription Id.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the subscription state.
        /// </summary>
        public string SubscriptionState { get; set; }

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
