// <copyright file="TokenServiceHttpClientProviderOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Options for the token service HTTP client provider.
    /// </summary>
    public class TokenServiceHttpClientProviderOptions : IHttpClientProviderOptions
    {
        /// <inheritdoc/>
        public string BaseAddress { get; set; }
    }
}
