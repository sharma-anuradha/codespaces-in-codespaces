// <copyright file="GitHubFixedPlansMapper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

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
        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubFixedPlansMapper"/> class.
        /// </summary>
        /// <param name="currentLocationProvider">The current location provider.</param>
        /// <param name="frontEndAppSettings">The Front End Settings.</param>
        public GitHubFixedPlansMapper(
            ICurrentLocationProvider currentLocationProvider,
            FrontEndAppSettings frontEndAppSettings)
        {
            CurrentLocationProvider = Requires.NotNull(currentLocationProvider, nameof(currentLocationProvider));
            FrontEndAppSettings = Requires.NotNull(frontEndAppSettings, nameof(frontEndAppSettings));
        }

        private ICurrentLocationProvider CurrentLocationProvider { get; }

        private FrontEndAppSettings FrontEndAppSettings { get; }

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
        /// Maps values in the parameter to preselected values from the configuration.
        /// </summary>
        /// <param name="createCloudEnvironmentBody">The parameter containing all the values relevant to the map.</param>
        /// <param name="request">The HTTP Request triggering this call, used to check if the GitHub auth handler was ran.</param>
        public void ApplyValuesWhenGitHubTokenIsUsed(CreateCloudEnvironmentBody createCloudEnvironmentBody, HttpRequest request)
        {
            request.Headers.TryGetValue(GitHubAuthenticationHandler.GitHubAuthenticationHandlerHeader, out StringValues headerValue);
            if (!headerValue.Any(x => string.Equals(bool.TrueString, x, StringComparison.OrdinalIgnoreCase)))
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

            createCloudEnvironmentBody.PlanId = plan.Plan.ResourceId;
        }
    }
}
