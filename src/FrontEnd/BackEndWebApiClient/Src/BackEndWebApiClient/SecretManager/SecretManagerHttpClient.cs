// <copyright file="SecretManagerHttpClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.SecretManager
{
    /// <summary>
    /// Http client for interacting with backend for secret management.
    /// </summary>
    public class SecretManagerHttpClient : HttpClientBase, ISecretManagerHttpContract
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecretManagerHttpClient"/> class.
        /// </summary>
        /// <param name="httpClientProvider">The backend http client provider.</param>
        public SecretManagerHttpClient(IHttpClientProvider<BackEndHttpClientProviderOptions> httpClientProvider)
            : base(httpClientProvider)
        {
        }

        /// <inheritdoc/>
        public Task AddOrUpdateSecreFiltersAsync(
            Guid resourceId,
            Guid secretId,
            IDictionary<SecretFilterType, string> secretFilters,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<SecretResult> CreateSecretAsync(
            Guid resourceId,
            CreateSecretBody createSecretBody,
            IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            var uri = SecretManagerHttpContract.GetCreateSecretUri(resourceId);
            return await SendAsync<CreateSecretBody, SecretResult>(SecretManagerHttpContract.CreateSecretMethod, uri, createSecretBody, logger);
        }

        /// <inheritdoc/>
        public Task DeleteSecretAsync(
            Guid resourceId,
            Guid secretId,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteSecretFilterAsync(
            Guid resourceId,
            Guid secretId,
            SecretFilterType secretFilterType,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ResourceSecretsResult>> GetSecretsAsync(
            IEnumerable<Guid> resourceIds,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(resourceIds, nameof(resourceIds));
            var uri = SecretManagerHttpContract.GetGetSecretsUri(resourceIds);
            return await SendAsync<IEnumerable<Guid>, IEnumerable<ResourceSecretsResult>>(SecretManagerHttpContract.GetSecretsMethod, uri, resourceIds, logger);
        }

        /// <inheritdoc/>
        public Task<SecretResult> UpdateSecretAsync(
            Guid resourceId,
            Guid secretId,
            UpdateSecretBody updateSecretBody,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
