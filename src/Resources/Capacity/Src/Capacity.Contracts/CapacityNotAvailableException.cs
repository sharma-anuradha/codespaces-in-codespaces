// <copyright file="CapacityNotAvailableException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// Indicates that the requested SKU is not available in any subscription.
    /// </summary>
    public class CapacityNotAvailableException : CapacityException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityNotAvailableException"/> class.
        /// </summary>
        /// <param name="location">The requested location.</param>
        /// <param name="quotas">The required quotas.</param>
        public CapacityNotAvailableException(AzureLocation location, IEnumerable<string> quotas)
            : this(location, quotas, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityNotAvailableException"/> class.
        /// </summary>
        /// <param name="location">The requested location.</param>
        /// <param name="quotas">The required quotas.</param>
        /// <param name="inner">The inner exception.</param>
        public CapacityNotAvailableException(AzureLocation location, IEnumerable<string> quotas, Exception inner)
            : base($"The required Azure capacity is not available for any subscription in location '{location}' for one or more of the following quotas: {MakeQuotaList(quotas)}", inner)
        {
            Quotas = quotas ?? Enumerable.Empty<string>();
            Location = location;
        }

        /// <summary>
        /// Gets the azure quota names.
        /// </summary>
        public IEnumerable<string> Quotas { get; }

        /// <summary>
        /// Gets the azure location.
        /// </summary>
        public AzureLocation Location { get; }

        private static string MakeQuotaList(IEnumerable<string> quotas)
        {
            quotas = quotas ?? Enumerable.Empty<string>();
            return string.Join(", ", quotas);
        }
    }
}
