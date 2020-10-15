// <copyright file="ResourcePoolOSDiskDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    /// OSDisk pool details.
    /// </summary>
    public class ResourcePoolOSDiskDetails : ResourcePoolResourceDetails
    {
        private const string ResourceType = "OSDisk";

        /// <summary>
        /// Gets or Sets the target OS.
        /// </summary>
        public ComputeOS OS { get; set; }

        /// <summary>
        /// Gets or sets the sku family.
        /// </summary>
        public string SkuFamily { get; set; }

        /// <summary>
        /// Gets or sets the target Compute Agent Image.
        /// </summary>
        public string VmAgentImageName { get; set; }

        /// <summary>
        /// Gets or sets the target Compute Agent Image Family.
        /// </summary>
        public string VmAgentImageFamilyName { get; set; }

        /// <summary>
        /// Gets or sets the disk size.
        /// </summary>
        public int? DiskSize { get; set; }

        /// <inheritdoc/>
        public override string GetPoolDefinition()
        {
            return $"{ResourceType}__{SkuName}__{Location}".GetDeterministicHashCode();
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
                [ResourcePoolDimensionsKeys.SizeInGB] = DiskSize?.ToString(),
            };
        }

        /// <inheritdoc/>
        public override string GetPoolVersionDefinition()
        {
            return $"{SkuName}__{Location}__{ImageFamilyName}__{ImageName}_{VmAgentImageFamilyName}_{VmAgentImageName}".GetDeterministicHashCode();
        }
    }
}
