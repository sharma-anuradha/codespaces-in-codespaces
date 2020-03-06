// <copyright file="ProfileUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Contracts
{
    /// <summary>
    /// <see cref="Profile"/> utility functions.
    /// </summary>
    public static class ProfileUtils
    {
        /// <summary>
        /// Checks that the user is able to know that a SKU exists.
        /// </summary>
        /// <param name="profile">The current user (or null if no user is signed in).</param>
        /// <param name="sku">The sku.</param>
        /// <returns>True if the user is able to know that the SKU exists.</returns>
        public static bool IsSkuVisibleToProfile(Profile profile, ICloudEnvironmentSku sku)
        {
            if (sku.ComputeOS == ComputeOS.Windows)
            {
                if (sku.SkuName.Equals("internalWindows") ||
                    sku.SkuName.Equals("internal64Server") ||
                    sku.SkuName.Equals("internal32Server"))
                {
                    return profile != null && profile.IsWindowsSkuInternalUser();
                }

                return profile != null && profile.IsWindowsSkuPreviewUser();
            }

            return true;
        }
    }
}
