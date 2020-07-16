// <copyright file="HttpContextCurrentIdentityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Reactive.Disposables;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// HttpContextCurrent IdentityProvider.
    /// </summary>
    public class HttpContextCurrentIdentityProvider : ICurrentIdentityProvider
    {
        private static readonly ClaimsPrincipal AnonymousPrincipal = new ClaimsPrincipal(new VsoAnonymousClaimsIdentity(new ClaimsIdentity()));
        private static readonly string HttpContextCurrentBearerTokenKey = $"{nameof(HttpContextCurrentUserProvider)}-UserToken";

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextCurrentIdentityProvider"/> class.
        /// </summary>
        /// <param name="contextAccessor">Target context accessor.</param>
        /// <param name="identityContextAccessor">Target identity context accessor.</param>
        public HttpContextCurrentIdentityProvider(
            IHttpContextAccessor contextAccessor,
            IIdentityContextAccessor identityContextAccessor)
        {
            HttpContextAccessor = Requires.NotNull(contextAccessor, nameof(contextAccessor));
            IdentityContextAccessor = identityContextAccessor;
        }

        /// <inheritdoc/>
        public VsoClaimsIdentity Identity
        {
            get
            {
                var vsoClaimsIdentity = HttpContextAccessor?.HttpContext?.User.Identity as VsoClaimsIdentity;
                vsoClaimsIdentity ??= IdentityContextAccessor?.IdentityContext?.Identity;

                return vsoClaimsIdentity ?? (VsoClaimsIdentity)AnonymousPrincipal.Identity;
            }
        }

        /// <inheritdoc/>
        public string BearerToken
        {
            get { return HttpContextAccessor?.HttpContext?.Items[HttpContextCurrentBearerTokenKey] as string; }
        }

        /// <summary>
        /// Gets identity context accessor.
        /// </summary>
        protected IIdentityContextAccessor IdentityContextAccessor { get; }

        /// <summary>
        /// Gets context Accessor.
        /// </summary>
        protected IHttpContextAccessor HttpContextAccessor { get; }

        /// <inheritdoc/>
        public void SetBearerToken(string token)
        {
            HttpContextAccessor.HttpContext.Items[HttpContextCurrentBearerTokenKey] = token;
        }

        /// <inheritdoc/>
        public IDisposable SetScopedIdentity(VsoClaimsIdentity identity, UserIdSet userIdSet = default)
        {
            if (IdentityContextAccessor.IdentityContext == null)
            {
                IdentityContextAccessor.IdentityContext = new IdentityContext();
            }
            else
            {
                throw new IdentityValidationException("An identity is already set for the current context.");
            }

            IdentityContextAccessor.IdentityContext.Identity = identity;
            IdentityContextAccessor.IdentityContext.UserIdSet = userIdSet;

            return Disposable.Create(() =>
            {
                IdentityContextAccessor.IdentityContext = null;
            });
        }
    }
}
