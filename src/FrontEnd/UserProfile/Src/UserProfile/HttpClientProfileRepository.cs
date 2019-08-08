﻿// <copyright file="HttpClientProfileRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// An <see cref="IProfileRepository"/> that remotes to the Live Share profile service.
    /// </summary>
    public class HttpClientProfileRepository : IProfileRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientProfileRepository"/> class.
        /// </summary>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="httpClientProvider">The http client provider.</param>
        public HttpClientProfileRepository(
            ICurrentUserHttpClientProvider<ProfileHttpClientProviderOptions> httpClientProvider)
        {
            HttpClientProvider = Requires.NotNull(httpClientProvider, nameof(httpClientProvider));
        }

        private IHttpClientProvider HttpClientProvider { get; }

        /// <inheritdoc/>
        public async Task<Profile> GetCurrentUserProfileAsync(IDiagnosticsLogger logger)
        {
            _ = logger;

            var response = await HttpClientProvider.HttpClient.GetAsync("profile");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return null;
            }

            await response.ThrowIfFailedAsync();

            var profile = await response.Content.ReadAsAsync<Profile>();

            return profile;
        }
    }
}
