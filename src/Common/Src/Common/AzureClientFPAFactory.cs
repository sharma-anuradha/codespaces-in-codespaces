// <copyright file="AzureClientFPAFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Identity.Client;
using Microsoft.Rest;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Build Azure Client.
    /// </summary>
    public class AzureClientFPAFactory : AzureClientFactoryBase, IAzureClientFPAFactory
    {
        private const string FirstPartyAppAuthority = "https://login.windows.net";

        private static readonly Regex TenantIdRegex =
            new Regex("authorization_uri=\"[a-z]*://[^/]*/([^\"]*)\"");

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureClientFPAFactory"/> class.
        /// </summary>
        /// <param name="firstPartyAppSettings">first party app settings.</param>
        /// <param name="firstPartyCertReader">first party certificate reader.</param>
        public AzureClientFPAFactory(
            FirstPartyAppSettings firstPartyAppSettings,
            IFirstPartyCertificateReader firstPartyCertReader)
        {
            FirstPartyAppSettings = Requires.NotNull(firstPartyAppSettings, nameof(firstPartyAppSettings));
            FirstPartyCertificateReader = Requires.NotNull(firstPartyCertReader, nameof(firstPartyCertReader));
        }

        private FirstPartyAppSettings FirstPartyAppSettings { get; }

        private IFirstPartyCertificateReader FirstPartyCertificateReader { get; }

        /// <inheritdoc/>
        public Task<IAzure> GetAzureClientAsync(string subscriptionId, string azureAppId, string azureAppKey, string azureTenantId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IComputeManagementClient> GetComputeManagementClient(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IStorageManagementClient> GetStorageManagementClient(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IKeyVaultManagementClient> GetKeyVaultManagementClient(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IResourceManagementClient> GetResourceManagementClient(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<IAzure> GetAzureClientAsync(Guid subscriptionId, IDiagnosticsLogger logger)
        {
            try
            {
                var creds = await GetFPACredsAsync(subscriptionId, logger);
                var azure = Microsoft.Azure.Management.Fluent.Azure.Authenticate(creds)
                    .WithSubscription(subscriptionId.ToString());

                return azure;
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(subscriptionId.ToString(), ex);
            }
        }

        /// <inheritdoc/>
        public async Task<INetworkManagementClient> GetNetworkManagementClient(Guid subscriptionId, IDiagnosticsLogger logger)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var restClient = await CreateFPARestClientAsync(subscriptionId, logger);
                var azureClient = new NetworkManagementClient(restClient)
                {
                    SubscriptionId = azureSubscriptionId,
                };
                return azureClient;
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        private async Task<(string Secret, string TenantId)> GetFPAToken(Guid subscriptionId, IDiagnosticsLogger logger)
        {
            var cert = await FirstPartyCertificateReader.GetApiFirstPartyAppCertificate(logger);

            var subTenantId = await GetTenantIdForSubscription(subscriptionId);
            var certificate = new X509Certificate2(cert.RawBytes);
            var builder = ConfidentialClientApplicationBuilder
                .Create(FirstPartyAppSettings.ApiFirstPartyAppId)
                .WithAuthority(FirstPartyAppAuthority, subTenantId, true)
                .WithCertificate(certificate)
                .Build();

            var token = await builder
                .AcquireTokenForClient(new[] { FirstPartyAppSettings.Scope })
                .WithAuthority(FirstPartyAppAuthority, subTenantId, true)
                .WithSendX5C(true)
                .ExecuteAsync();

            return (token.AccessToken, subTenantId);
        }

        private async Task<string> GetTenantIdForSubscription(Guid subscriptionId)
        {
            // True for vnet caches so we can use a prod vnet in stage
            string frontendUri = "https://management.azure.com";

            using (var client = new HttpClient()) // Unauthenticated client...
            {
                string apiVersion = "2015-01-01";
                var actionUri = frontendUri + "/subscriptions/" + subscriptionId + "?api-version=" + apiVersion;
                var response = await client.GetAsync(actionUri);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException($"The subscription {subscriptionId} could not be found");
                }
                else if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    throw new InvalidOperationException($"We expect this GET subscription call to fail with Unauthorized, not {response.StatusCode}!");
                }

                if (!response.Headers.Contains("WWW-Authenticate"))
                {
                    throw new InvalidOperationException("We require a WWW-Authenticate response header to get the tenantID!");
                }

                var wwwAuthenticate = GetHeaderValue(response.Headers, "WWW-Authenticate");
                var regexMatch = TenantIdRegex.Match(wwwAuthenticate);
                if (!regexMatch.Success)
                {
                    throw new InvalidOperationException("Failed to parse the WWW-Authenticate response header for the tenantID!");
                }

                var tenantId = regexMatch.Groups[1].Value;
                if (!Guid.TryParse(tenantId, out var tenantGuid))
                {
                    throw new InvalidOperationException("The tenantID we parsed from the WWW-Authenticate response header isn't a valid GUID!");
                }

                return tenantId;
            }
        }

        private string GetHeaderValue(HttpHeaders headers, string headerName)
        {
            IEnumerable<string> values = null;
            headers?.TryGetValues(headerName, out values);
            return values?.FirstOrDefault();
        }

        private async Task<RestClient> CreateFPARestClientAsync(Guid subscriptionId, IDiagnosticsLogger logger)
        {
            var creds = await GetFPACredsAsync(subscriptionId, logger);

            var restClient = RestClient.Configure()
                .WithEnvironment(creds.Environment)
                .WithCredentials(creds)
                .WithDelegatingHandler(new ProviderRegistrationDelegatingHandler(creds))
                .Build();

            return restClient;
        }

        private async Task<AzureCredentials> GetFPACredsAsync(Guid subscriptionId, IDiagnosticsLogger logger)
        {
            var (secret, tenantId) = await GetFPAToken(subscriptionId, logger);
            var tokenCreds = new TokenCredentials(secret);
            var creds = new AzureCredentials(
                tokenCreds,
                default,
                tenantId,
                AzureEnvironment.AzureGlobalCloud);
            return creds;
        }
    }
}
