// <copyright file="CurrentUserProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

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
            ConfidentialClient = ConfidentialClientApplicationBuilder.Create(appSettings.AuthClientId)
               .WithAuthority(AzureCloudInstance.AzurePublic, appSettings.AuthTenant)
               .WithClientSecret(appSettings.AuthClientSecret)
               .Build();

            Scopes = new List<string> { $"api://{appSettings.AuthAudience}/.default" };
        }

        private AuthenticationResult Result { get; set; }

        private IList<string> Scopes { get; }

        private IConfidentialClientApplication ConfidentialClient { get; }

        /// <inheritdoc/>
        public async Task<string> GetBearerToken()
        {
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
                throw;
            }
        }
    }
}
