// <copyright file="ProfileHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <inheritdoc/>
    public class ProfileHttpClientProvider
        : CurrentUserHttpClientProvider<ProfileHttpClientProviderOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileHttpClientProvider"/> class.
        /// </summary>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="options">The user profile options.</param>
        public ProfileHttpClientProvider(
            ICurrentUserProvider currentUserProvider,
            IOptions<ProfileHttpClientProviderOptions> options)
            : base(currentUserProvider, options)
        {
        }
    }
}
