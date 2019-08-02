// <copyright file="ServicePrincipal.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog
{
    /// <inheritdoc/>
    public class ServicePrincipal : IServicePrincipal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServicePrincipal"/> class.
        /// </summary>
        /// <param name="clientId">The service principal client id (appid).</param>
        /// <param name="clientSecretKeyvaultSecretId">The keyvault secret id for the service principal client secret.</param>
        /// <param name="tenantId">The service principal tenant id.</param>
        /// <param name="secretResolver">A callback that resolves <paramref name="clientSecretKeyvaultSecretId"/>.</param>
        public ServicePrincipal(
            string clientId,
            string clientSecretKeyvaultSecretId,
            string tenantId,
            Func<string, Task<string>> secretResolver)
        {
            Requires.NotNullOrEmpty(clientId, nameof(clientId));
            Requires.NotNullOrEmpty(clientSecretKeyvaultSecretId, nameof(clientSecretKeyvaultSecretId));
            Requires.NotNullOrEmpty(tenantId, nameof(tenantId));
            Requires.NotNull(secretResolver, nameof(secretResolver));

            ClientId = clientId;
            ClientSecretKeyvaultSecretId = clientSecretKeyvaultSecretId;
            TenantId = tenantId;
            SecretResolver = secretResolver;
        }

        /// <inheritdoc/>
        public string ClientId { get; }

        /// <inheritdoc/>
        public string TenantId { get; }

        private string ClientSecretKeyvaultSecretId { get;  }

        private Func<string, Task<string>> SecretResolver { get; }

        /// <inheritdoc/>
        public async Task<string> GetServicePrincipalClientSecret()
        {
            return await SecretResolver(ClientSecretKeyvaultSecretId).ConfigureAwait(false);
        }
    }
}
