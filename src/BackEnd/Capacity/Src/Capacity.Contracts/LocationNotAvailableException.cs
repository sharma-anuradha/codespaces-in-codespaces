// <copyright file="LocationNotAvailableException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// Indicates that the requested SKU is not available in any subscription.
    /// </summary>
    public class LocationNotAvailableException : CapacityException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocationNotAvailableException"/> class.
        /// </summary>
        /// <param name="location">The requested location.</param>
        public LocationNotAvailableException(AzureLocation location)
            : this(location, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationNotAvailableException"/> class.
        /// </summary>
        /// <param name="location">The requested location.</param>
        /// <param name="inner">The inner exception.</param>
        public LocationNotAvailableException(AzureLocation location, Exception inner)
            : base($"There is no Azure subscription available for location '{location}'.", inner)
        {
            Location = location;
        }

        /// <summary>
        /// Gets the azure location.
        /// </summary>
        public AzureLocation Location { get; }
    }
}
