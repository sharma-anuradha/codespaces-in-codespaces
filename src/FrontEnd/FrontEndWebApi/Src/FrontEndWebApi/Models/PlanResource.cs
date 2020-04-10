// <copyright file="PlanResource.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// JSON body properties from RPSaaS.
    /// </summary>
    public class PlanResource
    {
        /// <summary>
        /// Gets or sets the plan resource properties.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "properties")]
        public PlanResourceProperties Properties { get; set; }

        /// <summary>
        /// Gets or sets the resource id.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the provisioining state.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "provisioningState")]
        public string ProvisioningState { get; set; }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "tags")]
        public IDictionary<string, string> Tags { get; set; }
    }
}
