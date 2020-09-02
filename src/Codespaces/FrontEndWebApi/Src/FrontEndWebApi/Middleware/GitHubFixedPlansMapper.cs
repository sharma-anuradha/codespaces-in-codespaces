// <copyright file="GitHubFixedPlansMapper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware
{
    /// <summary>
    /// This class changes the incoming parameters of an action calling it
    /// and selects the pre-defined plan and other relevant parameters.
    /// If any action is to be performed, is controlled by a header that gets
    /// set in <see cref="GitHubAuthenticationHandler"/>. Specifically, this mapper
    /// expects <see cref="GitHubAuthenticationHandler.GitHubAuthenticationHandlerHeader"/>.
    /// </summary>
    public class GitHubFixedPlansMapper
    {
        private readonly JsonSerializer jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubFixedPlansMapper"/> class.
        /// </summary>
        /// <param name="currentLocationProvider">The current location provider.</param>
        /// <param name="frontEndAppSettings">The Front End Settings.</param>
        /// <param name="githubApiHttpClientProvider">The GitHub API client.</param>
        public GitHubFixedPlansMapper(
            ICurrentLocationProvider currentLocationProvider,
            FrontEndAppSettings frontEndAppSettings,
            IGithubApiHttpClientProvider githubApiHttpClientProvider)
        {
            CurrentLocationProvider = Requires.NotNull(currentLocationProvider, nameof(currentLocationProvider));
            FrontEndAppSettings = Requires.NotNull(frontEndAppSettings, nameof(frontEndAppSettings));
            GithubApiHttpClientProvider = Requires.NotNull(githubApiHttpClientProvider, nameof(githubApiHttpClientProvider));

            this.jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
            });
        }

        private ICurrentLocationProvider CurrentLocationProvider { get; }

        private FrontEndAppSettings FrontEndAppSettings { get; }

        private IGithubApiHttpClientProvider GithubApiHttpClientProvider { get; }

        /// <summary>
        /// Returns the <see cref="VsoPlan"/> we should be using. It uses the <see cref="CurrentLocationProvider"/>
        /// to determine the nearest stamp and retrieves the mapped plan to the stamp.
        ///
        /// </summary>
        /// <returns>Returns a very stripped down instance of the <see cref="VsoPlan"/> object.</returns>
        public VsoPlan GetPlanToUse()
        {
            AzureLocation currentLocation = CurrentLocationProvider.CurrentLocation;
            if (!FrontEndAppSettings.GitHubProxySettingsByLocation.TryGetValue(currentLocation.ToString(), out GitHubProxySettings settings))
            {
                // it's possible, we don't have a plan in this location (although that is a configuration error)
                // but since this is temporary, we can default to the first one, then
                var settingsPair = FrontEndAppSettings.GitHubProxySettingsByLocation.FirstOrDefault();
                if (settingsPair.Value == null)
                {
                    return null;
                }

                // we need this later, so let's set it
                currentLocation = Enum.Parse<AzureLocation>(settingsPair.Key);
                settings = settingsPair.Value;
            }

            var planInfo = new VsoPlanInfo()
            {
                Location = currentLocation,
                Subscription = settings.SubscriptionId,
                ResourceGroup = settings.ResourceGroup,
                Name = settings.PlanName,
                ProviderNamespace = settings.ProviderNamespace,
            };

            return new VsoPlan()
            {
                Id = string.Empty,
                Plan = planInfo,
            };
        }

        /// <summary>
        /// When the authentication happens through GitHub, the callback is used, to change the value of
        /// planId.
        /// </summary>
        /// <param name="planIdModifier">The callback to use with the new plan id.</param>
        /// <param name="request">The HTTP Request triggering this call, used to check if the GitHub auth handler was ran.</param>
        public void ApplyValuesWhenGitHubTokenIsUsed(Action<string> planIdModifier, HttpRequest request)
        {
            Requires.NotNull(planIdModifier, nameof(planIdModifier));
            request.Headers.TryGetValue(GitHubAuthenticationHandler.GitHubAuthenticationHandlerHeader, out StringValues headerValue);
            if (headerValue.All(x => string.IsNullOrEmpty(x)))
            {
                // it appears we aren't in a GitHub auth session
                return;
            }

            // TODO: feature flag (anvod)
            var plan = this.GetPlanToUse();

            if (plan == null)
            {
                return;
            }

            planIdModifier(plan.Plan.ResourceId);
        }

        /// <summary>
        /// Determines if the current user is a Microsoft internal user.
        /// </summary>
        /// <param name="request">The HTTP Request.</param>
        /// <param name="user">The User making the request.</param>
        /// <returns>Return true if the user belongs to the Microsoft org. If the current request
        /// is not authorized through a GitHub token, this will immediately return false.</returns>
        public async Task<bool> IsMicrosoftInternalUserAsync(HttpRequest request, ClaimsPrincipal user)
        {
            request.Headers.TryGetValue(GitHubAuthenticationHandler.GitHubAuthenticationHandlerHeader, out StringValues headerValue);
            var token = headerValue.FirstOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var username = user.FindFirst(CustomClaims.Username)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }

            try
            {
                var httpClient = GithubApiHttpClientProvider.HttpClient;
                var orgRequest = new HttpRequestMessage(HttpMethod.Get, $"/orgs/microsoft/members/{username}");
                orgRequest.Headers.Authorization = new AuthenticationHeaderValue("token", token);
                var response = await httpClient.SendAsync(orgRequest);

                return response.StatusCode == HttpStatusCode.NoContent;
            }
            catch
            {
                return false;
            }
        }
    }
}
