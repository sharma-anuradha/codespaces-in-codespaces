// <copyright file="ServicePrincipal.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <inheritdoc/>
    public class ServicePrincipal : IServicePrincipal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServicePrincipal"/> class.
        /// </summary>
        /// <param name="servicePrincipalOptions">The service principal settings.</param>
        /// <param name="secretProvider">The secret provider.</param>
        public ServicePrincipal(
            IOptions<ServicePrincipalOptions> servicePrincipalOptions,
            ISecretProvider secretProvider)
            : this(
                  Requires.NotNull(servicePrincipalOptions, nameof(servicePrincipalOptions)).Value.ServicePrincipalSettings,
                  Requires.NotNull(secretProvider, nameof(SecretProvider)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServicePrincipal"/> class.
        /// </summary>
        /// <param name="servicePrincipalSettings">The service principal settings.</param>
        /// <param name="secretProvider">The secret provider.</param>
        public ServicePrincipal(
            ServicePrincipalSettings servicePrincipalSettings,
            ISecretProvider secretProvider)
            : this(
                  Requires.NotNull(servicePrincipalSettings, nameof(servicePrincipalSettings)).ClientId,
                  servicePrincipalSettings.ClientSecretName,
                  servicePrincipalSettings.TenantId,
                  Requires.NotNull(secretProvider, nameof(secretProvider)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServicePrincipal"/> class.
        /// </summary>
        /// <param name="clientId">The service principal client id (appid).</param>
        /// <param name="clientSecretName">The keyvault secret id for the service principal client secret.</param>
        /// <param name="tenantId">The service principal tenant id.</param>
        /// <param name="secretProvider">The secret provider.</param>
        public ServicePrincipal(
            string clientId,
            string clientSecretName,
            string tenantId,
            ISecretProvider secretProvider)
        {
            Requires.NotNullOrEmpty(clientId, nameof(clientId));
            Requires.NotNullOrEmpty(clientSecretName, nameof(clientSecretName));
            Requires.NotNullOrEmpty(tenantId, nameof(tenantId));
            Requires.NotNull(secretProvider, nameof(secretProvider));

            ClientId = clientId;
            ClientSecretName = clientSecretName;
            TenantId = tenantId;
            SecretProvider = secretProvider;
        }

        /// <inheritdoc/>
        public string ClientId { get; }

        /// <inheritdoc/>
        public string TenantId { get; }

        private string ClientSecretName { get;  }

        private ISecretProvider SecretProvider { get; }

        /// <inheritdoc/>
        public async Task<string> GetServicePrincipalClientSecretAsync()
        {
            return await SecretProvider.GetSecretAsync(ClientSecretName);
        }
    }
}
