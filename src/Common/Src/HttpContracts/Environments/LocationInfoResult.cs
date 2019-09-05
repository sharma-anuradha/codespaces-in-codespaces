// <copyright file="LocationInfoResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// The location info REST API result.
    /// </summary>
    public class LocationInfoResult
    {
        /// <summary>
        /// Gets or sets a list of all SKUs available at the location.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "skus")]
        public string[] Skus { get; set; }
    }
}
