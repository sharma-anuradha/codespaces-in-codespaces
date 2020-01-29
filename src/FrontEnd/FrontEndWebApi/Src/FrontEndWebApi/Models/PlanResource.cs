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
        [JsonProperty(Required = Required.Default, PropertyName = "properties")]
        public PlanResourceProperties Properties { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "provisioningState")]
        public string ProvisioningState { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        public string Location { get; set; }

        [JsonProperty(Required = Required.Default, PropertyName = "tags")]
        public IDictionary<string, string> Tags { get; set; }
    }
}
