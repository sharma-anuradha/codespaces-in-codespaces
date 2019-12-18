// <copyright file="ICurrentUserProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// Handlers user identity actions for the current context.
    /// </summary>
    public interface ICurrentUserProvider
    {
        /// <summary>
        /// Sets the current user's bearer token.
        /// </summary>
        /// <remarks>
        /// TODO: token for which API/Audience.
        /// </remarks>
        /// <param name="token">The token value.</param>
        void SetBearerToken(string token);

        /// <summary>
        /// Gets the current user's bearer token.
        /// </summary>
        /// <returns>The token value.</returns>
        /// <remarks>
        /// TODO: token for which API/Audience.
        /// </remarks>
        string GetBearerToken();

        /// <summary>
        /// Gets the current users's profile.
        /// </summary>
        /// <returns>The <see cref="Profile"/> instance.</returns>
        Profile GetProfile();

        /// <summary>
        /// Sets the current user's profile.
        /// </summary>
        /// <param name="profile">The <see cref="Profile"/> instance.</param>
        void SetProfile(Profile profile);

        /// <summary>
        /// Gets the current user's profile id.
        /// </summary>
        /// <returns>The profile id.</returns>
        [Obsolete("Use GetCurrentUserIdSet instead.", true)]
        string GetProfileId();

        /// <summary>
        /// Gets the current user profile provider id.
        /// </summary>
        /// <returns>The profile provider id.</returns>
        [Obsolete("Use GetCurrentUserIdSet instead.", true)]
        string GetProfileProviderId();

        /// <summary>
        /// Gets the current user canonical user id.
        /// </summary>
        /// <returns>The profile provider id.</returns>
        string GetCanonicalUserId();

        /// <summary>
        /// Gets the current user's user id set.
        /// </summary>
        /// <returns>A new <see cref="UserIdSet"/> instance.</returns>
        UserIdSet GetCurrentUserIdSet();

        /// <summary>
        /// Gets the current user's id map key.
        /// </summary>
        /// <returns>The id map key, of the form "{email}:{tenantId}".</returns>
        string GetIdMapKey();

        /// <summary>
        /// Set the user  id values.
        /// </summary>
        /// <param name="idMapKey">The identity map key.</param>
        /// <param name="canonicalUserId">The v2 user id.</param>
        /// <param name="profileId">The profile id.</param>
        /// <param name="profileProviderId">The profile provider id.</param>
        void SetUserIds(string idMapKey, string canonicalUserId, string profileId, string profileProviderId);
    }
}
