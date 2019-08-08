// <copyright file="IProfileCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// A profile cache.
    /// </summary>
    public interface IProfileCache
    {
        /// <summary>
        /// Gets a profile.
        /// </summary>
        /// <param name="profileId">The profile id.</param>
        /// <returns>Returns a <see cref="Profile"/> or null.</returns>
        Profile GetProfile(string profileId);

        /// <summary>
        /// Gets a profile.
        /// </summary>
        /// <param name="profile">The profile object.</param>
        void SetProfile(Profile profile);
    }
}
