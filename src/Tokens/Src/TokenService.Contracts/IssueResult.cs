// <copyright file="IssueResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Contracts
{
    /// <summary>
    /// Result from issuing or exchanging a token.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IssueResult
    {
        /// <summary>
        /// Gets or sets the issued token.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Token { get; set; } = null!;
    }
}
