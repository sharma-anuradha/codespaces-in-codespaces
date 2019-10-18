// <copyright file="ForwardingBearerAuthMessageHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Providers;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.HttpClient.Common
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
            // Pull out the current token
            var authToken = await CurrentUserProvider.GetBearerToken();

            // We can only conintue if we have it
            if (string.IsNullOrEmpty(authToken))
            {
                throw new Exception("No auth token was provided by the CurrentUserProvider.");
            }

            // Add the actual header
            request.Headers.Add(AuthHeaderName, AuthTokenPrefix + authToken);

            // Continue
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
