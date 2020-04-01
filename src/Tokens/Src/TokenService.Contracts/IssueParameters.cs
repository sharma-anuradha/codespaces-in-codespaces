// <copyright file="IssueParameters.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
        [JsonProperty(Required = Required.Always)]
        public IDictionary<string, string> Claims { get; set; } = null!;
    }
}
