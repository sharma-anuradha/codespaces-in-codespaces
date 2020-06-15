// <copyright file="AzureResourceUsage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// Specifies the the quota limit and current uage.
    /// </summary>
    public class AzureResourceUsage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureResourceUsage"/> class.
        /// </summary>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="serviceType">Service type.</param>
        /// <param name="azureLocation">The azure location.</param>
        /// <param name="quota">The azure quota name.</param>
        /// <param name="limit">The quota limit.</param>
        /// <param name="currentValue">The quota current value.</param>
        public AzureResourceUsage(
            string subscriptionId,
            ServiceType serviceType,
            AzureLocation azureLocation,
            string quota,
            long limit,
            long currentValue)
        {
            Requires.NotNullOrEmpty(subscriptionId, nameof(subscriptionId));
            Requires.NotNullOrEmpty(quota, nameof(quota));
            Requires.Argument(limit >= 0, nameof(limit), "Must be greater than or equal to zero");
            Requires.Argument(currentValue >= 0, nameof(currentValue), "Must be greater than or equal to zero");
            SubscriptionId = subscriptionId;
            Location = azureLocation;
            ServiceType = serviceType;
            Quota = quota;
            Limit = limit;
            CurrentValue = currentValue;
        }

        /// <summary>
        /// Gets the subscription id for this usage.
        /// </summary>
        public string SubscriptionId { get; }

        /// <summary>
        /// Gets the azure location for this usage.
        /// </summary>
        public AzureLocation Location { get; }

        /// <summary>
        /// Gets the the service type for this quota.
        /// </summary>
        public ServiceType ServiceType { get; }

        /// <summary>
        /// Gets the auzre quota name.
        /// </summary>
        public string Quota { get; }

        /// <summary>
        /// Gets the azure limit for this quota.
        /// </summary>
        public long Limit { get; }

        /// <summary>
        /// Gets the current value for this quota.
        /// </summary>
        public long CurrentValue { get; }
    }
}
