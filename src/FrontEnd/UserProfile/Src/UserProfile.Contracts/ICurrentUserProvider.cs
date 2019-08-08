// <copyright file="ICurrentUserProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
        string GetProfileId();
    }
}
