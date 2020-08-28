// <copyright file="SystemResourceCountByDimensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    /// Represents a count of system resources grouped by <see cref="SystemResourceDimensions"/>.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SystemResourceCountByDimensions : SystemResourceDimensions
    {
        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        public int Count { get; set; }
    }
}
