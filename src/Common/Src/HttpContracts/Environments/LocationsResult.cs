// <copyright file="LocationsResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// The locations REST API result.
    /// </summary>
    public class LocationsResult
    {
        /// <summary>
        /// Gets or sets the current location this instance of the service is running in.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "current")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation Current { get; set; }

        /// <summary>
        /// Gets or sets a list of all available locations globally for the service.
        /// </summary>
        [JsonProperty(
            Required = Required.Always,
            PropertyName = "available",
            ItemConverterType = typeof(StringEnumConverter))]
        public AzureLocation[] Available { get; set; }
    }
}
