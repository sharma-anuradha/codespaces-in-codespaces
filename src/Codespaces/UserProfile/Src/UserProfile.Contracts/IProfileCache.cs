// <copyright file="IProfileCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

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
        Task<Profile> GetProfileAsync(string profileId);

        /// <summary>
        /// Gets a profile.
        /// </summary>
        /// <param name="profileId">The profile ID.</param>
        /// <param name="lazyProfile">The lazy profile object.</param>
        void SetProfile(string profileId, Lazy<Task<Profile>> lazyProfile);
    }
}
