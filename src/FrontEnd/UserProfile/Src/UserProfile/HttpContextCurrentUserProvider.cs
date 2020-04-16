// <copyright file="HttpContextCurrentUserProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Security.Claims;
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
        private static readonly ClaimsPrincipal AnonomousUser = new ClaimsPrincipal(new VsoAnonymousClaimsIdentity(new ClaimsIdentity()));
        private static readonly string HttpContextCurrentUserTokenKey = $"{nameof(HttpContextCurrentUserProvider)}-UserToken";
        private static readonly string HttpContextCurrentUserIdMapKey = $"{nameof(HttpContextCurrentUserProvider)}-IdMapKey";
        private static readonly string HttpContextCurrentUserIdSetKey = $"{nameof(HttpContextCurrentUserProvider)}-UserIdSet";

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

        /// <inheritdoc/>
        public string BearerToken
        {
            get { return ContextAccessor?.HttpContext?.Items[HttpContextCurrentUserTokenKey] as string; }
        }

        /// <inheritdoc/>
        public VsoClaimsIdentity Identity
        {
            get
            {
                var vsoClaimsIdentity = ContextAccessor?.HttpContext?.User.Identity as VsoClaimsIdentity;
                if (vsoClaimsIdentity == null)
                {
                    return (VsoClaimsIdentity)AnonomousUser.Identity;
                }

                return vsoClaimsIdentity;
            }
        }

        /// <inheritdoc/>
        public Profile Profile
        {
            get
            {
                var profileId = ContextAccessor?.HttpContext.GetCurrentUserProfileId();
                return !string.IsNullOrEmpty(profileId) ? ProfileCache.GetProfile(profileId) : null;
            }
        }

        /// <inheritdoc/>
        public string CanonicalUserId
        {
            get { return ContextAccessor?.HttpContext?.GetCurrentUserCanonicalUserId(); }
        }

        /// <inheritdoc/>
        public UserIdSet CurrentUserIdSet
        {
            get { return ContextAccessor?.HttpContext?.Items[HttpContextCurrentUserIdSetKey] as UserIdSet; }
        }

        /// <inheritdoc/>
        public string IdMapKey
        {
            get { return ContextAccessor?.HttpContext?.Items[HttpContextCurrentUserIdMapKey] as string; }
        }

        private IProfileCache ProfileCache { get; }

        private IHttpContextAccessor ContextAccessor { get; }

        /// <inheritdoc/>
        public void SetBearerToken(string token)
        {
            ContextAccessor.HttpContext.Items[HttpContextCurrentUserTokenKey] = token;
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
        public void SetUserIds(string idMapKey, string canonicalUserId, string profileId, string profileProviderId)
        {
            var httpContext = ContextAccessor?.HttpContext;
            if (httpContext != null)
            {
                // Save the id map key and the canonicaluserid.
                httpContext.Items[HttpContextCurrentUserIdMapKey] = idMapKey;
                httpContext.SetCurrentUserCanonicalUserId(canonicalUserId);

                // Don't set profileId or profileProviderId because these come from the identity map
                // and do not necessarily match the current Profile.
                // Instead, just set the userIdSet.
                var userIdSet = new UserIdSet(canonicalUserId, profileId, profileProviderId);
                httpContext.Items[HttpContextCurrentUserIdSetKey] = userIdSet;
            }
        }
    }
}
