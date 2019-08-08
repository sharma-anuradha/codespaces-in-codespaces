// <copyright file="BackEndHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient
{
    /// <inheritdoc/>
    public class BackEndHttpClientProvider
        : CurrentUserHttpClientProvider<BackEndHttpClientProviderOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackEndHttpClientProvider"/> class.
        /// </summary>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="options">The user profile options.</param>
        public BackEndHttpClientProvider(
            ICurrentUserProvider currentUserProvider,
            IOptions<BackEndHttpClientProviderOptions> options)
            : base(currentUserProvider, options)
        {
        }
    }
}
