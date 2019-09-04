// <copyright file="AuthHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveshareAuthentication
{
    /// <inheritdoc/>
    public class AuthHttpClientProvider
        : CurrentUserHttpClientProvider<AuthHttpClientProviderOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthHttpClientProvider"/> class.
        /// </summary>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="options">The authentication http client options.</param>
        public AuthHttpClientProvider(
            ICurrentUserProvider currentUserProvider,
            IOptions<AuthHttpClientProviderOptions> options)
            : base(currentUserProvider, options)
        {
        }
    }
}
