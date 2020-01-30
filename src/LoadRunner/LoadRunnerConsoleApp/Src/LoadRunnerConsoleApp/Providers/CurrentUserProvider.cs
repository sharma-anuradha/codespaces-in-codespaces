// <copyright file="CurrentUserProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Providers
{
    /// <summary>
    /// Auth provider which provides access to current user.
    /// </summary>
    public class CurrentUserProvider : ICurrentUserProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentUserProvider"/> class.
        /// </summary>
        /// <param name="appSettings">Target app settings.</param>
        public CurrentUserProvider(AppSettings appSettings)
        {
            AppSettings = appSettings;
            ConfidentialClient = ConfidentialClientApplicationBuilder.Create(appSettings.AuthClientId)
               .WithAuthority(AzureCloudInstance.AzurePublic, appSettings.AuthTenant)
               .WithClientSecret(appSettings.AuthClientSecret)
               .Build();

            Scopes = new List<string> { $"api://{appSettings.AuthAudience}/.default" };
        }

        private AuthenticationResult Result { get; set; }

        private AppSettings AppSettings { get; set; }

        private IList<string> Scopes { get; }

        private IConfidentialClientApplication ConfidentialClient { get; }

        /// <inheritdoc/>
        public async Task<string> GetBearerToken()
        {
            // Refresh token flow
            if (!string.IsNullOrEmpty(AppSettings.AuthRefreshToken))
            {
                var client = new HttpClient();

                var uri = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
                var pairs = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("client_id", "a3037261-2c94-4a2e-b53f-090f6cdd712a"),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", AppSettings.AuthRefreshToken),
                };

                var content = new FormUrlEncodedContent(pairs);

                var response = await client.PostAsync(uri, content);

                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();
                var payload = JsonConvert.DeserializeObject<TokenExchangeResponse>(body);

                return payload.AccessToken;
            }

            // Application auth flow
            try
            {
                // If we couldn't get it from cache, go to souce
                if (Result == null || DateTime.UtcNow.AddMinutes(-1) > Result.ExpiresOn.UtcDateTime)
                {
                    Result = await ConfidentialClient.AcquireTokenForClient(Scopes)
                        .ExecuteAsync();
                }

                return Result.AccessToken;
            }
            catch (Exception e)
            {
                if (e == null)
                {
                    return null;
                }

                throw;
            }
        }
    }
}
