// <copyright file="GitHubApiGatewayProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System.Security.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Gateways;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Providers
{
    /// <summary>
    /// Provides an instance of the GitHubApiGateway.
    /// </summary>
    public class GitHubApiGatewayProvider
    {
        private readonly ICurrentLocationProvider currentLocationProvider;
        private readonly IHttpContextAccessor contextAccessor;

        public GitHubApiGatewayProvider(
            IHttpContextAccessor contextAccessor,
            ICurrentLocationProvider currentLocationProvider)
        {
            this.currentLocationProvider = Requires.NotNull(currentLocationProvider, nameof(currentLocationProvider));
            this.contextAccessor = Requires.NotNull(contextAccessor, nameof(contextAccessor));
        }

        public GitHubApiGateway New()
        {
            if (!GitHubAuthenticationHandler.IsInGitHubAuthenticatedSession(
                contextAccessor.HttpContext.Request,
                out string token))
            {
                throw new InvalidCredentialException("Missing or invalid GitHub Token.");
            }

            return new GitHubApiGateway(currentLocationProvider, token);
        }
    }
}
