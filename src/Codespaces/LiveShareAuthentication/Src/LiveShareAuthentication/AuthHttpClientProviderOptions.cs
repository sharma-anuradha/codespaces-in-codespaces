// <copyright file="AuthHttpClientProviderOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveshareAuthentication
{
    /// <inheritdoc/>
    public class AuthHttpClientProviderOptions
        : IHttpClientProviderOptions
    {
        /// <inheritdoc/>
        public string BaseAddress { get; set; }
    }
}
