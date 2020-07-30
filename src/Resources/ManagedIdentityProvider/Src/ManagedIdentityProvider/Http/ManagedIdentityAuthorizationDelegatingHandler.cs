// <copyright file="ManagedIdentityAuthorizationDelegatingHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// A delegating handler to append Azure Credentials.
    /// </summary>
    public class ManagedIdentityAuthorizationDelegatingHandler : DelegatingHandler
    {
        private const string LogBaseName = "managed_identity_authorization_delegating_handler";

        private const string ApiVersion = "2015-08-31-PREVIEW";

        private readonly IFirstPartyTokenBuilder tokenBuilder;
        private readonly IDiagnosticsLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedIdentityAuthorizationDelegatingHandler"/> class.
        /// </summary>
        /// <param name="innerHandler">The inner handler.</param>
        /// <param name="tokenBuilder">The token provider.</param>
        /// <param name="logger">The logger instance.</param>
        public ManagedIdentityAuthorizationDelegatingHandler(
            HttpMessageHandler innerHandler,
            IFirstPartyTokenBuilder tokenBuilder,
            IDiagnosticsLogger logger)
            : base(innerHandler)
        {
            this.tokenBuilder = tokenBuilder;
            this.logger = logger;
        }

        /// <inheritdoc/>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri = AppendApiVersion(request.RequestUri);

            var resourceToken = await GetManagedIdentityResourceTokenAsync(request.RequestUri, logger, cancellationToken);

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", resourceToken);

            return await base.SendAsync(request, cancellationToken);
        }

        private Task<string> GetManagedIdentityResourceTokenAsync(Uri identityUri, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_get_resource_token",
                async (childLogger) =>
                {
                    // first, make an unauthenticated call to MSI to get the AuthorityUrl, Tenant and Resource information.
                    var unauthenticatedResponse = await base.SendAsync(
                        new HttpRequestMessage
                        {
                            Method = HttpMethod.Get,
                            RequestUri = identityUri,
                        },
                        cancellationToken);

                    // parse the authority and resource values from the http bearer challenge, example:
                    var wwwAuthenticateHeaderValues = HttpBearerChallengeUtils.ParseWwwAuthenticateHeader(unauthenticatedResponse);

                    // second, get an access token from Azure AD using our first-party Resource Provider applicationId and certificate
                    string authority = wwwAuthenticateHeaderValues["authorization"];
                    string resource = wwwAuthenticateHeaderValues["resource"];

                    var token = await tokenBuilder.GetMsiResourceTokenAsync(authority, resource, logger);

                    return token.AccessToken;
                });
        }

        private Uri AppendApiVersion(Uri identityUri)
        {
            // you must append the x-ms-identity-url with the api-version you wish to use
            var queryString = HttpUtility.ParseQueryString(identityUri.Query);
            queryString["api-version"] = ApiVersion;

            var uriBuilder = new UriBuilder(identityUri);
            uriBuilder.Query = queryString.ToString();

            return uriBuilder.Uri;
        }
    }
}
