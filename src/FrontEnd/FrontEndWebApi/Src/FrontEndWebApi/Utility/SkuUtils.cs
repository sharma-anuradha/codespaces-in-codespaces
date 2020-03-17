// <copyright file="SkuUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Utility
{
    /// <summary>
    /// sku utility functions can be accessed here.
    /// </summary>
    public class SkuUtils : ISkuUtils
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SkuUtils"/> class.
        /// </summary>
        /// <param name="logger">The logger to be used.</param>
        /// <param name="systemConfiguration">SystemConfiguration settings.</param>
        public SkuUtils(IDiagnosticsLogger logger, ISystemConfiguration systemConfiguration)
        {
            Logger = logger;
            SystemConfiguration = systemConfiguration;
        }

        /// <summary>
        /// Gets IDiagnosticsLogger to be used for logging.
        /// </summary>
        public IDiagnosticsLogger Logger { get; }

        /// <summary>
        /// Gets the supported feature flags from db.
        /// </summary>
        public ISystemConfiguration SystemConfiguration { get; }

        /// <summary>
        /// Verifies if the given Sku is enabled based on subscription/ user profile.
        /// </summary>
        /// <param name="sku">Sku info.</param>
        /// <param name="planInfo">plan info.</param>
        /// <param name="profile">User profile.</param>
        /// <returns> A boolean value true, if the sku is enabled. <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<bool> IsVisible(ICloudEnvironmentSku sku, VsoPlanInfo planInfo, Profile profile)
        {
            // Adding null Check
            if (sku == null)
            {
                return false;
            }

            var isEnabled = ProfileUtils.IsSkuVisibleToProfile(profile, sku);

            /*
             * If planInfo is available, then check for feature flags enabled
             * As of now it is available only for Basic Linux Sku at subscription level.
             */
            if (planInfo != null)
            {
                foreach (var featureFlag in sku.SupportedFeatureFlags)
                {
                    var isTurnedOn = await SystemConfiguration.GetSubscriptionValueAsync(featureFlag, planInfo.Subscription, Logger, false);
                    if (!isTurnedOn && isEnabled)
                    {
                        isEnabled = false;
                    }
                }
            }

            return isEnabled;
        }
    }
}
