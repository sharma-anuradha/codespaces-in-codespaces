// <copyright file="TokenExchangeResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Providers
{
    /// <summary>
    /// Token Exchange Response.
    /// </summary>
    public class TokenExchangeResponse
    {
        /// <summary>
        /// Gets or sets access token.
        /// </summary>
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets refresh token.
        /// </summary>
        [JsonProperty(PropertyName = "refresh_token")]
        public string RefreshToken { get; set; }
    }
}
