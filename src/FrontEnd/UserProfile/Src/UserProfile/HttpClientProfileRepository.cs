// <copyright file="HttpClientProfileRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// An <see cref="IProfileRepository"/> that remotes to the Live Share profile service.
    /// </summary>
    public class HttpClientProfileRepository : IProfileRepository
    {
        private const string LogBaseName = "httpclientprofilerepository";

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientProfileRepository"/> class.
        /// </summary>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="httpClientProvider">The http client provider.</param>
        public HttpClientProfileRepository(
            IHttpClientProvider<ProfileHttpClientProviderOptions> httpClientProvider)
        {
            HttpClientProvider = Requires.NotNull(httpClientProvider, nameof(httpClientProvider));
        }

        private IHttpClientProvider HttpClientProvider { get; }

        /// <inheritdoc/>
        public Task<Profile> GetCurrentUserProfileAsync(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_getcurrentuserprofile",
                async (childLogger) =>
                {
                    var response = await childLogger.RetryOperationScopeAsync(
                        $"{LogBaseName}_getcurrentuserprofile_getprofileprograms",
                        async (retryLogger) =>
                        {
                            var operationTimeout = TimeSpan.FromSeconds(5);
                            var cts = new CancellationTokenSource();
                            cts.CancelAfter(operationTimeout);

                            return await HttpClientProvider.HttpClient.GetAsync("profile?scope=programs", cts.Token);
                        });

                    logger.AddClientHttpResponseDetails(response);

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return null;
                    }

                    await response.ThrowIfFailedAsync();

                    var content = await response.Content.ReadAsStringAsync();
                    var profile = JsonConvert.DeserializeObject<Profile>(content);
                    return profile;
                });
        }
    }
}
