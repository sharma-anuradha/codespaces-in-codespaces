// <copyright file="GitHubFixedPlansMapper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.Linq;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Newtonsoft.Json;

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
            FrontEndAppSettings frontEndAppSettings)
        {
            CurrentLocationProvider = Requires.NotNull(currentLocationProvider, nameof(currentLocationProvider));
            FrontEndAppSettings = Requires.NotNull(frontEndAppSettings, nameof(frontEndAppSettings));            

            this.jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
            });
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
    }
}
