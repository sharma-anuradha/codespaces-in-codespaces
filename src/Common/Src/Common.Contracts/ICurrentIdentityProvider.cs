// <copyright file="ICurrentIdentityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// Current identity provider.
    /// </summary>
    public interface ICurrentIdentityProvider
    {
        /// <summary>
        /// Gets the current user's bearer token.
        /// </summary>
        /// <returns>The token value.</returns>
        /// <remarks>
        /// TODO: token for which API/Audience.
        /// </remarks>
        string BearerToken { get; }

        /// <summary>
        /// Gets the current identity.
        /// </summary>
        /// <returns>Vso Claims Identity.</returns>
        VsoClaimsIdentity Identity { get; }

        /// <summary>
        /// Sets the current user's bearer token.
        /// </summary>
        /// <remarks>
        /// TODO: token for which API/Audience.
        /// </remarks>
        /// <param name="token">The token value.</param>
        void SetBearerToken(string token);

        /// <summary>
        /// Sets the current identity.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <param name="userIdSet">Optional userIdSet.</param>
        /// <returns>An <see cref="IDisposable"/> to clear the identity in context.</returns>
        IDisposable SetScopedIdentity(VsoClaimsIdentity identity, UserIdSet userIdSet = default);
    }
}
