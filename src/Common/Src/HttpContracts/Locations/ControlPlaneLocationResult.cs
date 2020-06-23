// <copyright file="ControlPlaneLocationResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts
{
    /// <summary>
    /// The control planes locations REST API result.
    /// </summary>
    public class ControlPlaneLocationResult
    {
        /// <summary>
        /// Gets or sets current azure location.
        /// </summary>
        [JsonProperty(
            Required = Required.Always,
            PropertyName = "location")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the service dns hostname.
        /// </summary>
        [JsonProperty(
            Required = Required.Always,
            PropertyName = "dnsHostName")]
        public string DnsHostName { get; set; }
    }
}
