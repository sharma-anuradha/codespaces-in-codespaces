// <copyright file="AccountResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts
{
    /// <summary>
    /// The account REST API result.
    /// </summary>
    public class AccountResult
    {
        [JsonProperty(Required = Required.Always, PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "subscription")]
        public string Subscription { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "resourceGroup")]
        public string ResourceGroup { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Azure location (region) the account resource is in.
        /// All environments in the account must be in the same location.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation Location { get; set; }
    }
}
