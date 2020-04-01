// <copyright file="ValidateParameters.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Contracts
{
    /// <summary>
    /// Request parameters for validating a token.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ValidateParameters
    {
        /// <summary>
        /// Gets or sets the token to be validated.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Token { get; set; } = null!;
    }
}
