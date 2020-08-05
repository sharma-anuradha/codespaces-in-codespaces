// <copyright file="IssueParameters.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Security.Claims;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Contracts
{
    /// <summary>
    /// Request parameters for issuing a token.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IssueParameters
    {
        /// <summary>
        /// Gets or sets the token claims, as an array of `Claim` objects.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(ClaimsConverter))]
        public Claim[] Claims { get; set; } = null!;
    }
}
