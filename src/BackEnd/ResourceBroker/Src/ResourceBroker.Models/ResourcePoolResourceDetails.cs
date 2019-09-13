// <copyright file="ResourcePoolResourceDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// Resource details for scaling input.
    /// </summary>
    public abstract class ResourcePoolResourceDetails
    {
        /// <summary>
        /// Gets or sets the target resource sku name.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the target location.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the target image name.
        /// </summary>
        public string ImageFamilyName { get; set; }

        /// <summary>
        /// Gets or sets the target image value.
        /// </summary>
        public string ImageName { get; set; }

        /// <summary>
        /// Gets the hash that makes up the definition of the pool.
        /// </summary>
        /// <returns>The hash code of the pool.</returns>
        public abstract string GetPoolDefinition();

        /// <summary>
        /// Gets the versioned hash that makes up the definition of the pool.
        /// </summary>
        /// <returns>The versioned hash code of the pool.</returns>
        public abstract string GetPoolVersionDefinition();
    }
}
