// <copyright file="IssueParameters.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
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
        /// Gets or sets the token claims.
        /// </summary>
        /// <remarks>
        /// Only string and integer claim values are supported.
        /// </remarks>
        [JsonProperty(Required = Required.Always)]
        public IDictionary<string, object> Claims { get; set; } = null!;
    }
}
