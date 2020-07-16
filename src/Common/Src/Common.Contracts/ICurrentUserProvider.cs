// <copyright file="ICurrentUserProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// Handlers user identity actions for the current context.
    /// </summary>
    public interface ICurrentUserProvider : ICurrentIdentityProvider
    {
        /// <summary>
        /// Gets the current user canonical user id.
        /// </summary>
        /// <returns>The profile provider id.</returns>
        string CanonicalUserId { get; }

        /// <summary>
        /// Gets the current user's user id set.
        /// </summary>
        /// <returns>A new <see cref="UserIdSet"/> instance.</returns>
        UserIdSet CurrentUserIdSet { get; }

        /// <summary>
        /// Gets the current user's id map key.
        /// </summary>
        /// <returns>The id map key, of the form "{email}:{tenantId}".</returns>
        string IdMapKey { get; }

        /// <summary>
        /// Sets the current user's profile.
        /// </summary>
        /// <param name="lazyProfile">The <see cref="UserProfile.Profile"/> instance.</param>
        /// <param name="profileId">The profile ID.</param>
        /// <param name="profileProviderId">The profile provider ID.</param>
        void SetProfile(Lazy<Task<Profile>> lazyProfile, string profileId, string profileProviderId);

        /// <summary>
        /// Gets the current users's profile.
        /// </summary>
        /// <returns>The <see cref="UserProfile.Profile"/> instance.</returns>
        Task<Profile> GetProfileAsync();

        /// <summary>
        /// Set the user  id values.
        /// </summary>
        /// <param name="idMapKey">The identity map key.</param>
        /// <param name="userIdSet">The user ID set.</param>
        void SetUserIds(string idMapKey, UserIdSet userIdSet);
    }
}
