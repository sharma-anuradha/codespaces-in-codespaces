// <copyright file="FPAAccessTokenHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http
{
    /// <summary>
    /// A handler that adds the FPA Authorization header.
    /// </summary>
    public class FPAAccessTokenHandler : DelegatingHandler
    {
        private readonly IFirstPartyTokenBuilder tokenBuilder;
        private readonly IDiagnosticsLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FPAAccessTokenHandler"/> class.
        /// </summary>
        /// <param name="innerHandler">The inner handler.</param>
        /// <param name="tokenBuilder">The first party token builder.</param>
        /// <param name="logger">The logger instance.</param>
        public FPAAccessTokenHandler(
            HttpMessageHandler innerHandler,
            IFirstPartyTokenBuilder tokenBuilder,
            IDiagnosticsLogger logger)
            : base(innerHandler)
        {
            this.tokenBuilder = tokenBuilder;
            this.logger = logger;
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var authToken = (await tokenBuilder.GetFpaTokenAsync(logger)).AccessToken;

            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
