// <copyright file="ResourcePoolStorageDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// Storage details for scaling input.
    /// </summary>
    public class ResourcePoolStorageDetails : ResourcePoolResourceDetails
    {
        /// <summary>
        /// Gets or sets the size of the share in Gb.
        /// </summary>
        public int SizeInGB { get; set; }

        /// <inheritdoc/>
        public override string GetPoolDefinition()
        {
            return $"{SkuName}__{Location}__{ImageFamilyName}__{SizeInGB}".GetDeterministicHashCode();
        }

        /// <inheritdoc/>
        public override IDictionary<string, string> GetPoolDimensions()
        {
            return new Dictionary<string, string>
            {
                [ResourcePoolDimensionsKeys.SizeInGB] = SizeInGB.ToString(),
                [ResourcePoolDimensionsKeys.SkuName] = SkuName,
                [ResourcePoolDimensionsKeys.Location] = Location.ToString(),
                [ResourcePoolDimensionsKeys.ImageFamilyName] = ImageFamilyName,
                [ResourcePoolDimensionsKeys.ImageName] = ImageName,
            };
        }

        /// <inheritdoc/>
        public override string GetPoolVersionDefinition()
        {
            return $"{SkuName}__{Location}__{ImageFamilyName}__{SizeInGB}__{ImageName}".GetDeterministicHashCode();
        }
    }
}
