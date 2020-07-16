// <copyright file="HttpContextProfileCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

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
        public void SetProfile(string profileId, Lazy<Task<Profile>> lazyProfile)
        {
            Requires.NotNull(lazyProfile, nameof(lazyProfile));
            ContextAccessor.HttpContext.Items[BuildKey(profileId)] = lazyProfile;
        }

        /// <inheritdoc/>
        public async Task<Profile> GetProfileAsync(string profileId)
        {
            var result = default(Profile);

            if (!string.IsNullOrEmpty(profileId))
            {
                var lazyProfile = ContextAccessor.HttpContext.Items[BuildKey(profileId)] as Lazy<Task<Profile>>;
                return await lazyProfile.Value;
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
