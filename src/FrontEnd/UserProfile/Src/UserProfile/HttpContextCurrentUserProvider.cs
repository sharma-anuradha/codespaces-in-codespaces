// <copyright file="HttpContextCurrentUserProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.AspNetCore.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// A current user provider based on the current HTTP context.
    /// </summary>
    public class HttpContextCurrentUserProvider : ICurrentUserProvider
    {
        private static readonly string HttpContextCurrentUserIdKey = $"{nameof(HttpContextCurrentUserProvider)}-UserId";
        private static readonly string HttpContextCurrentUserTokenKey = $"{nameof(HttpContextCurrentUserProvider)}-UserToken";

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextCurrentUserProvider"/> class.
        /// </summary>
        /// <param name="profileCache">The profile cache.</param>
        /// <param name="contextAccessor">The http context accessor.</param>
        public HttpContextCurrentUserProvider(
            IProfileCache profileCache,
            IHttpContextAccessor contextAccessor)
        {
            ProfileCache = Requires.NotNull(profileCache, nameof(profileCache));
            ContextAccessor = Requires.NotNull(contextAccessor, nameof(contextAccessor));
        }

        private IProfileCache ProfileCache { get; }

        private IHttpContextAccessor ContextAccessor { get; }

        /// <inheritdoc/>
        public void SetBearerToken(string token)
        {
            ContextAccessor.HttpContext.Items[HttpContextCurrentUserTokenKey] = token;
        }

        /// <inheritdoc/>
        public string GetBearerToken()
        {
            return ContextAccessor?.HttpContext?.Items[HttpContextCurrentUserTokenKey] as string;
        }

        /// <inheritdoc/>
        public Profile GetProfile()
        {
            var profileId = GetProfileId();
            return !string.IsNullOrEmpty(profileId) ? ProfileCache.GetProfile(profileId) : null;
        }

        /// <inheritdoc/>
        public void SetProfile(Profile profile)
        {
            Requires.NotNull(profile, nameof(profile));
            ProfileCache.SetProfile(profile);
            ContextAccessor.HttpContext.Items[HttpContextCurrentUserIdKey] = profile.Id;

            // TEMPORARY HACK. This needs to be reworked to use the VS SaaS SDK auth middleware
            // which is supposed to set this for us.
            // Needed for per-user throttling to work correctly.
            ContextAccessor.HttpContext.SetCurrentUserId(profile.ProviderId);
        }

        /// <inheritdoc/>
        public string GetProfileId()
        {
            return ContextAccessor?.HttpContext?.Items[HttpContextCurrentUserIdKey] as string;
        }
    }
}
