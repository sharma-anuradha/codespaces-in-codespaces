// <copyright file="GitHubApiGatewayProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
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
        private readonly IConfigurationReader configurationReader;

        public GitHubApiGatewayProvider(
            IHttpContextAccessor contextAccessor,
            ICurrentLocationProvider currentLocationProvider,
            IHostEnvironment hostEnvironment,            
            IConfigurationReader configurationReader)
        {
            this.currentLocationProvider = Requires.NotNull(currentLocationProvider, nameof(currentLocationProvider));
            this.contextAccessor = Requires.NotNull(contextAccessor, nameof(contextAccessor));
            this.hostEnvironment = Requires.NotNull(hostEnvironment, nameof(hostEnvironment));
            this.configurationReader = Requires.NotNull(configurationReader, nameof(configurationReader));
        }

        public async Task<bool> IsGitHubApiGatewayEnabled(IDiagnosticsLogger logger)
        {
            // check the header first, then go to the database
            if (contextAccessor
                ?.HttpContext
                ?.Request
                ?.Headers
                ?.ContainsKey("x-codespaces-enable-github-api") ?? false)
            {
                return true;
            }

            return await configurationReader.ReadFeatureFlagAsync<bool>(
                "github-gateway-enabled",
                logger.NewChildLogger(),
                false);
        }

        public async Task<GitHubApiGateway> NewAsync(IDiagnosticsLogger diagnosticsLogger)
        {
            return await diagnosticsLogger.OperationScopeAsync(
                "github_apigatewayprovider_new",
                async (logger) =>
                {
                    if (!await IsGitHubApiGatewayEnabled(logger))
                    {
                        logger.LogInfo("refusing_github_gateway_featureflag_disabled");
                        return null;
                    }

                    if (!GitHubAuthenticationHandler.IsInGitHubAuthenticatedSession(
                        contextAccessor.HttpContext.Request,
                        out string token))
                    {
                        throw new InvalidCredentialException("Missing or invalid GitHub Token.");
                    }

                    return new GitHubApiGateway(currentLocationProvider, hostEnvironment, token);
                });
        }
    }
}
