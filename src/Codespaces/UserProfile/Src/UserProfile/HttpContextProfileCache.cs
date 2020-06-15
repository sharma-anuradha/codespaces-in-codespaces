// <copyright file="HttpContextProfileCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// An <see cref="IProfileCache"/> based on the current HTTP context.
    /// </summary>
    public class HttpContextProfileCache : IProfileCache
    {
        private static readonly string HttpContextCurrentProfileKey = $"{nameof(HttpContextProfileCache)}-Profile";

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextProfileCache"/> class.
        /// </summary>
        /// <param name="contextAccessor">The http context accessor.</param>
        public HttpContextProfileCache(IHttpContextAccessor contextAccessor)
        {
            ContextAccessor = contextAccessor;
        }

        private IHttpContextAccessor ContextAccessor { get; }

        /// <inheritdoc/>
        public void SetProfile(Profile profile)
        {
            Requires.NotNull(profile, nameof(profile));
            ContextAccessor.HttpContext.Items[BuildKey(profile.Id)] = profile;
        }

        /// <inheritdoc/>
        public Profile GetProfile(string profileId)
        {
            var result = default(Profile);

            if (!string.IsNullOrEmpty(profileId))
            {
                result = ContextAccessor.HttpContext.Items[BuildKey(profileId)] as Profile;
            }

            return result;
        }

        private static string BuildKey(string id)
        {
            Requires.NotNullOrEmpty(id, nameof(id));
            return $"{HttpContextCurrentProfileKey}__{id}";
        }
    }
}
