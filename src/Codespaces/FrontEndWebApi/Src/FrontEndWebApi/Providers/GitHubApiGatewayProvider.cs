// <copyright file="GitHubApiGatewayProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.Security.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
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
        private readonly IHostEnvironment hostEnvironment;

        public GitHubApiGatewayProvider(
            IHttpContextAccessor contextAccessor,
            ICurrentLocationProvider currentLocationProvider,
            IHostEnvironment hostEnvironment)
        {
            this.currentLocationProvider = Requires.NotNull(currentLocationProvider, nameof(currentLocationProvider));
            this.contextAccessor = Requires.NotNull(contextAccessor, nameof(contextAccessor));
            this.hostEnvironment = Requires.NotNull(hostEnvironment, nameof(hostEnvironment));
        }

        public GitHubApiGateway New()
        {
            // HOTFIX for release 10/5 weekly release - disable GitHub API forking
            // To remove: delete this throw and uncomment the below code
            throw new InvalidOperationException("GitHubApiGatewayProvider is disabled");

            /*
            if (!GitHubAuthenticationHandler.IsInGitHubAuthenticatedSession(
                contextAccessor.HttpContext.Request,
                out string token))
            {
                throw new InvalidCredentialException("Missing or invalid GitHub Token.");
            }

            return new GitHubApiGateway(currentLocationProvider, hostEnvironment, token);
            */
        }
    }
}
