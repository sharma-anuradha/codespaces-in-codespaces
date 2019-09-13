// <copyright file="ResourcePoolComputeDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// Compute details for scaling input.
    /// </summary>
    public class ResourcePoolComputeDetails : ResourcePoolResourceDetails
    {
        /// <summary>
        /// Gets or Sets the target OS.
        /// </summary>
        public ComputeOS OS { get; set; }

        /// <inheritdoc/>
        public override string GetPoolDefinition()
        {
            return $"{SkuName}__{Location}__{ImageFamilyName}".GetDeterministicHashCode();
        }

        /// <inheritdoc/>
        public override string GetPoolVersionDefinition()
        {
            return $"{SkuName}__{Location}__{ImageFamilyName}__{ImageName}".GetDeterministicHashCode();
        }
    }
}
