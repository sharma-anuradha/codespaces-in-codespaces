// <copyright file="ResourcePoolComputeDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
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

        /// <summary>
        /// Gets or sets the sku family -- used for querying quotas.
        /// </summary>
        public string SkuFamily { get; set; }

        /// <summary>
        /// Gets or sets the number of cores -- used for querying quotas.
        /// </summary>
        public int Cores { get; set; }

        /// <summary>
        /// Gets or Sets the target Compute Agent Image.
        /// </summary>
        public string VmAgentImageName { get; set; }

        /// <summary>
        /// Gets or Sets the target Compute Agent Image Family.
        /// </summary>
        public string VmAgentImageFamilyName { get; set; }

        /// <inheritdoc/>
        public override string GetPoolDefinition()
        {
            return $"{SkuName}__{Location}__{ImageFamilyName}".GetDeterministicHashCode();
        }

        /// <inheritdoc/>
        public override IDictionary<string, string> GetPoolDimensions()
        {
            return new Dictionary<string, string>
            {
                [ResourcePoolDimensionsKeys.ComputeOS] = OS.ToString(),
                [ResourcePoolDimensionsKeys.SkuName] = SkuName,
                [ResourcePoolDimensionsKeys.Location] = Location.ToString(),
                [ResourcePoolDimensionsKeys.ImageFamilyName] = ImageFamilyName,
                [ResourcePoolDimensionsKeys.ImageName] = ImageName,
            };
        }

        /// <inheritdoc/>
        public override string GetPoolVersionDefinition()
        {
            return $"{SkuName}__{Location}__{ImageFamilyName}__{ImageName}_{VmAgentImageFamilyName}_{VmAgentImageName}".GetDeterministicHashCode();
        }
    }
}
