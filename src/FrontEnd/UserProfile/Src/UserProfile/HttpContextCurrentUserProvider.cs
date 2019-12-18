// <copyright file="HttpContextCurrentUserProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// A current user provider based on the current HTTP context.
    /// </summary>
    public class HttpContextCurrentUserProvider : ICurrentUserProvider
    {
        private static readonly string HttpContextCurrentUserTokenKey = $"{nameof(HttpContextCurrentUserProvider)}-UserToken";
        private static readonly string HttpContextCurrentUserIdMapKey = $"{nameof(HttpContextCurrentUserProvider)}-IdMapKey";

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
            ContextAccessor.HttpContext.SetCurrentUserProfileId(profile.Id);
            ContextAccessor.HttpContext.SetCurrentUserProfileProviderId(profile.ProviderId);
        }

        /// <inheritdoc/>
        public string GetProfileId()
        {
            return ContextAccessor?.HttpContext.GetCurrentUserProfileId();
        }

        /// <inheritdoc/>
        public string GetProfileProviderId()
        {
            return ContextAccessor?.HttpContext?.GetCurrentUserProfileProviderId();
        }

        /// <inheritdoc/>
        public string GetCanonicalUserId()
        {
            return ContextAccessor?.HttpContext?.GetCurrentUserCanonicalUserId();
        }

        /// <inheritdoc/>
        public UserIdSet GetCurrentUserIdSet()
        {
            return new UserIdSet(GetCanonicalUserId(), GetProfileId(), GetProfileProviderId());
        }

        /// <inheritdoc/>
        public string GetIdMapKey()
        {
            return ContextAccessor?.HttpContext?.Items[HttpContextCurrentUserIdMapKey] as string;
        }

        /// <inheritdoc/>
        public void SetUserIds(string idMapKey, string canonicalUserId, string profileId, string profileProviderId)
        {
            var httpContext = ContextAccessor?.HttpContext;
            if (httpContext != null)
            {
                httpContext.Items[HttpContextCurrentUserIdMapKey] = idMapKey;
                httpContext.SetCurrentUserCanonicalUserId(canonicalUserId);
                httpContext.SetCurrentUserProfileId(profileId);
                httpContext.SetCurrentUserProfileProviderId(profileProviderId);
            }
        }
    }
}
