// <copyright file="CloudEnvironmentCountByDimensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Represents a count of cloud environments grouped by <see cref="CloudEnvironmentDimensions"/>.
    /// </summary>
    public class CloudEnvironmentCountByDimensions : CloudEnvironmentDimensions
    {
        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "count")]
        public int Count { get; set; }
    }
}
