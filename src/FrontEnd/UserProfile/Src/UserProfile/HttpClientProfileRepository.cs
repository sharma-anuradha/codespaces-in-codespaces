// <copyright file="HttpClientProfileRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

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
            const string LogErrorMessage = "httpclientprofilerepository_getcurrentuserprofileasync_error";

            var response = await HttpClientProvider.HttpClient.GetAsync("profile?scope=programs");
            logger?.AddValue(LoggingConstants.HttpRequestUri, response.RequestMessage.RequestUri.AbsoluteUri);
            logger?.AddValue(LoggingConstants.HttpResponseStatus, response.StatusCode.ToString());

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                logger?.LogError(LogErrorMessage);
                return null;
            }

            try
            {
                await response.ThrowIfFailedAsync();
            }
            catch (Exception ex)
            {
                logger?.LogErrorWithDetail(LogErrorMessage, ex.Message);
                throw;
            }

            var profile = await response.Content.ReadAsAsync<Profile>();
            return profile;
        }
    }
}
