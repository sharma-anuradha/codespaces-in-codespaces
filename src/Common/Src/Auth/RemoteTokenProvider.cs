// <copyright file="RemoteTokenProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.TokenService.Client;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Token provider.
    /// </summary>
    public class RemoteTokenProvider : ITokenProvider
    {
        private readonly TokenServiceClient tokenClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteTokenProvider"/> class.
        /// </summary>
        /// <param name="servicePrincipal">Service principal.</param>
        /// <param name="authenticationSettings">Authentication settings.</param>
        /// <param name="httpClientProvider">HTTP client provider.</param>
        public RemoteTokenProvider(
            IServicePrincipal servicePrincipal,
            AuthenticationSettings authenticationSettings,
            IHttpClientProvider<TokenServiceHttpClientProviderOptions> httpClientProvider)
        {
            Requires.NotNull(servicePrincipal, nameof(servicePrincipal));
            Settings = Requires.NotNull(authenticationSettings, nameof(authenticationSettings));
            Requires.NotNull(httpClientProvider, nameof(httpClientProvider));

            ValidateTokenSettings(authenticationSettings.VmTokenSettings, nameof(authenticationSettings.VmTokenSettings));
            ValidateTokenSettings(authenticationSettings.VsSaaSTokenSettings, nameof(authenticationSettings.VsSaaSTokenSettings));
            ValidateTokenSettings(authenticationSettings.ConnectionTokenSettings, nameof(authenticationSettings.ConnectionTokenSettings));

            this.tokenClient = new TokenServiceClient(
                httpClientProvider.HttpClient,
                new ServicePrincipalIdentity(
                    servicePrincipal.TenantId,
                    servicePrincipal.ClientId,
                    servicePrincipal.GetClientSecretAsync));
        }

        /// <inheritdoc/>
        public AuthenticationSettings Settings { get; }

        /// <inheritdoc/>
        public async Task<string> IssueTokenAsync(
            string issuer,
            string audience,
            DateTime expires,
            IEnumerable<Claim> claims,
            IDiagnosticsLogger logger)
        {
            var payload = new JwtPayload(issuer, audience, claims, notBefore: null, expires);

            return await logger.OperationScopeAsync("sts_request_issue", async (innerLogger) =>
            {
                return await this.tokenClient.IssueAsync(payload, CancellationToken.None);
            });
        }

        private static void ValidateTokenSettings(TokenSettings tokenSettings, string fieldName)
        {
            Requires.NotNull(tokenSettings, fieldName);
            Requires.NotNullOrWhiteSpace(tokenSettings.Issuer, $"{fieldName}.{nameof(tokenSettings.Issuer)}");
            Requires.NotNullOrWhiteSpace(tokenSettings.Audience, $"{fieldName}.{nameof(tokenSettings.Audience)}");
        }
    }
}
