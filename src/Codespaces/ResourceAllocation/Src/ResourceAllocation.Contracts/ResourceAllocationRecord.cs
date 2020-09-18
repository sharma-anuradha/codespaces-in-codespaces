// <copyright file="ResourceAllocationRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation
{
    /// <summary>
    /// Represents a backend resource associated with a cloud environment.
    /// </summary>
    /// <remarks>
    /// See backend resource broker AllocateResult.
    /// </remarks>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ResourceAllocationRecord
    {
        /// <summary>
        /// Gets or sets the resource id token.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the Azure location.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the resource sku name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the created date and time.
        /// </summary>
        [JsonProperty]
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public ResourceType? Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether record is ready.
        /// </summary>
        [JsonProperty]
        public bool IsReady { get; set; }
    }
}
