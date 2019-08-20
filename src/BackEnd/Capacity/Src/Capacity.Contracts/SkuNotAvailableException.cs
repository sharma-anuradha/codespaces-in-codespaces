// <copyright file="SkuNotAvailableException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// Indicates that the requested SKU is not available in any subscription.
    /// </summary>
    public class SkuNotAvailableException : CapacityException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SkuNotAvailableException"/> class.
        /// </summary>
        /// <param name="skuName">The requested sku name.</param>
        /// <param name="location">The requested location.</param>
        public SkuNotAvailableException(string skuName, AzureLocation location)
            : this(skuName, location, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkuNotAvailableException"/> class.
        /// </summary>
        /// <param name="skuName">The requested sku name.</param>
        /// <param name="location">The requested location.</param>
        /// <param name="inner">The inner exception.</param>
        public SkuNotAvailableException(string skuName, AzureLocation location, Exception inner)
            : base($"The sku '${skuName}' is not available in any subscription.", inner)
        {
            SkuName = skuName;
            Location = location;
        }

        /// <summary>
        /// Gets the cloud environment sku name.
        /// </summary>
        public string SkuName { get; }

        /// <summary>
        /// Gets the azure location.
        /// </summary>
        public AzureLocation Location { get; }
    }
}
