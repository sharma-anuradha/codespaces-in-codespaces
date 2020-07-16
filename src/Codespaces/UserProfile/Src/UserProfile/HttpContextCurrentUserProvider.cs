// <copyright file="HttpContextCurrentUserProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// A current user provider based on the current HTTP context.
    /// </summary>
    public class HttpContextCurrentUserProvider : HttpContextCurrentIdentityProvider, ICurrentUserProvider
    {
        private static readonly string HttpContextCurrentUserIdMapKey = $"{nameof(HttpContextCurrentUserProvider)}-IdMapKey";
        private static readonly string HttpContextCurrentUserIdSetKey = $"{nameof(HttpContextCurrentUserProvider)}-UserIdSet";

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextCurrentUserProvider"/> class.
        /// </summary>
        /// <param name="profileCache">The profile cache.</param>
        /// <param name="httpContextAccessor">The http context accessor.</param>
        /// <param name="identityContextAccessor">The identity context accessor.</param>
        public HttpContextCurrentUserProvider(
            IProfileCache profileCache,
            IHttpContextAccessor httpContextAccessor,
            IIdentityContextAccessor identityContextAccessor)
            : base(httpContextAccessor, identityContextAccessor)
        {
            ProfileCache = Requires.NotNull(profileCache, nameof(profileCache));
        }

        /// <inheritdoc/>
        public string CanonicalUserId
        {
            get { return HttpContextAccessor?.HttpContext?.GetCurrentUserCanonicalUserId() ?? IdentityContextAccessor.IdentityContext?.UserIdSet?.CanonicalUserId; }
        }

        /// <inheritdoc/>
        public UserIdSet CurrentUserIdSet
        {
            get { return HttpContextAccessor?.HttpContext?.Items[HttpContextCurrentUserIdSetKey] as UserIdSet ?? IdentityContextAccessor.IdentityContext?.UserIdSet; }
        }

        /// <inheritdoc/>
        public string IdMapKey
        {
            get { return HttpContextAccessor?.HttpContext?.Items[HttpContextCurrentUserIdMapKey] as string; }
        }

        private IProfileCache ProfileCache { get; }

        /// <inheritdoc/>
        public async Task<Profile> GetProfileAsync()
        {
            var profileId = HttpContextAccessor?.HttpContext.GetCurrentUserProfileId();

            if (!string.IsNullOrEmpty(profileId))
            {
                return await ProfileCache.GetProfileAsync(profileId);
            }

            return null;
        }

        /// <inheritdoc/>
        public void SetProfile(Lazy<Task<Profile>> profile, string profileId, string profileProviderId)
        {
            Requires.NotNull(profile, nameof(profile));
            ProfileCache.SetProfile(profileId, profile);
            HttpContextAccessor.HttpContext.SetCurrentUserProfileId(profileId);
            HttpContextAccessor.HttpContext.SetCurrentUserProfileProviderId(profileProviderId);
        }

        /// <inheritdoc/>
        public void SetUserIds(string idMapKey, UserIdSet userIdSet)
        {
            var httpContext = HttpContextAccessor?.HttpContext;
            if (httpContext != null)
            {
                // Save the id map key and the canonicaluserid.
                httpContext.Items[HttpContextCurrentUserIdMapKey] = idMapKey;
                httpContext.SetCurrentUserCanonicalUserId(userIdSet.CanonicalUserId);

                // Don't set profileId or profileProviderId because these come from the identity map
                // and do not necessarily match the current Profile.
                // Instead, just set the userIdSet.
                httpContext.Items[HttpContextCurrentUserIdSetKey] = userIdSet;
            }
            else if (IdentityContextAccessor.IdentityContext != null)
            {
                IdentityContextAccessor.IdentityContext.UserIdSet = userIdSet;
            }
        }
    }
}
