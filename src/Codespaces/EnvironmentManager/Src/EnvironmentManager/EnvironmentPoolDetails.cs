// <copyright file="EnvironmentPoolDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Resource details for scaling input.
    /// </summary>
    public class EnvironmentPoolDetails
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
        /// Gets the hash that makes up the definition of the pool.
        /// </summary>
        /// <returns>The hash code of the pool.</returns>
        public string GetPoolDefinition()
        {
            return $"{SkuName}__{Location}".GetDeterministicHashCode();
        }
    }
}