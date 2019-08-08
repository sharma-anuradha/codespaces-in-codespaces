// <copyright file="ForwardingBearerAuthMessageHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http
{
    /// <summary>
    /// A message handlers that forwards the user's bearer token.
    /// </summary>
    public class ForwardingBearerAuthMessageHandler : DelegatingHandler
    {
        private const string AuthHeaderName = "Authorization";
        private const string AuthTokenPrefix = "Bearer ";

        /// <summary>
        /// Initializes a new instance of the <see cref="ForwardingBearerAuthMessageHandler"/> class.
        /// </summary>
        /// <param name="innerHandler">The inner handler.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        public ForwardingBearerAuthMessageHandler(
            HttpMessageHandler innerHandler,
            ICurrentUserProvider currentUserProvider)
            : base(innerHandler)
        {
            CurrentUserProvider = Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
        }

        private ICurrentUserProvider CurrentUserProvider { get; }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var authToken = CurrentUserProvider.GetBearerToken();
            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Add(AuthHeaderName, AuthTokenPrefix + authToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
