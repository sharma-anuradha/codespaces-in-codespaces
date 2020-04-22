// <copyright file="ServicePrincipalIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.VsSaaS.Services.TokenService.Client
{
    /// <summary>
    /// Identity parameters required to get an AAD token for a service principal.
    /// </summary>
    public class ServicePrincipalIdentity
    {
        private AuthenticationResult? authResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServicePrincipalIdentity"/> class.
        /// </summary>
        /// <param name="tenantId">Service principal tenant ID.</param>
        /// <param name="clientId">Service principal client ID.</param>
        /// <param name="clientSecretProvider">Callback for getting the service principal
        /// client secret.</param>
        public ServicePrincipalIdentity(
            string tenantId,
            string clientId,
            Func<Task<string>> clientSecretProvider)
        {
            Requires.NotNullOrEmpty(tenantId, nameof(tenantId));
            Requires.NotNullOrEmpty(clientId, nameof(clientId));
            Requires.NotNull(clientSecretProvider, nameof(clientSecretProvider));

            TenantId = tenantId;
            ClientId = clientId;
            ClientSecretProvider = clientSecretProvider;
        }

        /// <summary>
        /// Gets the service principal tenant ID.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// Gets the service principal client ID.
        /// </summary>
        public string ClientId { get; }

        private Func<Task<string>> ClientSecretProvider { get; }

        /// <summary>
        /// Gets an authentication header that authenticates this service principal.
        /// </summary>
        /// <param name="resource">Resource that this SP will authenticate to. This value is
        /// the audience of the auth token.</param>
        /// <returns>Authentication header including a token.</returns>
        /// <remarks>
        /// The obtained auth token is saved and re-used until it is near-expiry.
        /// </remarks>
        public async Task<AuthenticationHeaderValue?> GetAuthenticationHeaderAsync(string resource)
        {
            if (!(this.authResult?.ExpiresOn > DateTime.UtcNow.AddMinutes(5)))
            {
                string authority = $"https://login.windows.net/{TenantId}/";
                var authContext = new AuthenticationContext(authority, false);
                var clientSecret = await ClientSecretProvider();
                var clientCredential = new ClientCredential(ClientId, clientSecret);
                this.authResult = await authContext.AcquireTokenAsync(resource, clientCredential);
            }

            if (this.authResult?.AccessToken == null)
            {
                throw new UnauthorizedAccessException();
            }

            return new AuthenticationHeaderValue(
                this.authResult.AccessTokenType, this.authResult.AccessToken);
        }
    }
}
