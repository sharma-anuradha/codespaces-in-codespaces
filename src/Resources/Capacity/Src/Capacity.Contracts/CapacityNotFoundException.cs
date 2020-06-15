// <copyright file="CapacityNotFoundException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// Indicates that the requested SKU is not available in any subscription.
    /// </summary>
    public class CapacityNotFoundException : CapacityException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityNotFoundException"/> class.
        /// </summary>
        /// <param name="subscription">The azure subscription.</param>
        /// <param name="serviceType">The service type.</param>
        /// <param name="quota">The quota name.</param>
        /// <param name="location">The requested location.</param>
        /// <param name="inner">The inner exception.</param>
        public CapacityNotFoundException(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, string quota, Exception inner = null)
            : base($"The capacity record could not be found: {subscription?.DisplayName}/{location}/{serviceType}/{quota}", inner)
        {
            Subscription = subscription;
            Location = location;
            ServiceType = serviceType;
            Quota = quota;
        }

        /// <summary>
        /// Gets the azure subscription.
        /// </summary>
        public IAzureSubscription Subscription { get; }

        /// <summary>
        /// Gets the azure location.
        /// </summary>
        public AzureLocation Location { get; }

        /// <summary>
        /// Gets the service type.
        /// </summary>
        public ServiceType ServiceType { get; }

        /// <summary>
        /// Gets the azure quota name.
        /// </summary>
        public string Quota { get; }
    }
}
