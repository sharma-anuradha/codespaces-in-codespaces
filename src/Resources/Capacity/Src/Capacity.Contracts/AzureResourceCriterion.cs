// <copyright file="AzureResourceCriterion.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// A resource location criterion.
    /// </summary>
    public class AzureResourceCriterion
    {
        /// <summary>
        /// Gets or sets the quota service type.
        /// </summary>
        public ServiceType ServiceType { get; set; }

        /// <summary>
        /// Gets or sets the quota name.
        /// </summary>
        public string Quota { get; set; }

        /// <summary>
        /// Gets or sets the required amount.
        /// </summary>
        public long Required { get; set; }
    }
}
