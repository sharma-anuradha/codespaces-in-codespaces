// <copyright file="ISkuUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// An interface for Sku Utils.
    /// </summary>
    public interface ISkuUtils
    {
        /// <summary>
        /// Checks whether the specified Sku is enabled via user profile & feature flags.
        /// </summary>
        /// <param name="sku">Sku info.</param>
        /// <param name="planInfo">Plan info.</param>
        /// <param name="profile">User Profile info.</param>
        /// <returns>Boolean value.<see cref="Task"/> representing the asynchronous operation.</returns>
        Task<bool> IsVisible(ICloudEnvironmentSku sku, VsoPlanInfo planInfo, Profile profile);
    }
}
