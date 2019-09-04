// <copyright file="AuthHttpClientProviderOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveshareAuthentication
{
    /// <inheritdoc/>
    public class AuthHttpClientProviderOptions
        : ICurrentUserHttpClientProviderOptions
    {
        /// <inheritdoc/>
        public string BaseAddress { get; set; }
    }
}
