// <copyright file="SecretFilterBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager
{
    /// <summary>
    /// Secret filter http contract.
    /// </summary>
    public class SecretFilterBody
    {
        /// <summary>
        /// Gets or sets secret filter type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(Required = Required.Always, PropertyName = "type")]
        public SecretFilterType Type { get; set; }

        /// <summary>
        /// Gets or sets filter value.
        /// </summary>
        [StringLength(200, MinimumLength = 1)]
        [JsonProperty(Required = Required.Always, PropertyName = "value")]
        public string Value { get; set; }
    }
}
