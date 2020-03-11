// <copyright file="PlanResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts
{
    /// <summary>
    /// The plan REST API result.
    /// </summary>
    public class PlanResult
    {
        /// <summary>
        /// Gets or Sets the plan Id.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or Sets the Subscription info.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "subscription")]
        public string Subscription { get; set; }

        /// <summary>
        /// Gets or Sets the ResourceGroup info.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "resourceGroup")]
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or Sets the Plan Name.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Azure location (region) the plan resource is in.
        /// All environments in the plan must be in the same location.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation Location { get; set; }
    }
}
