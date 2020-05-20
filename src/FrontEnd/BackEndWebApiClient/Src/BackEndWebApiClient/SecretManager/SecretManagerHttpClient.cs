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
        public async Task DeleteSecretAsync(
            Guid resourceId,
            Guid secretId,
            IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            Requires.NotEmpty(secretId, nameof(secretId));
            var uri = SecretManagerHttpContract.GetDeleteSecretUri(resourceId, secretId);
            await SendRawAsync<object>(SecretManagerHttpContract.DeleteSecretMethod, uri, null, logger);
        }

        /// <inheritdoc/>
        public async Task<SecretResult> DeleteSecretFilterAsync(
            Guid resourceId,
            Guid secretId,
            SecretFilterType secretFilterType,
            IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            Requires.NotEmpty(secretId, nameof(secretId));
            var uri = SecretManagerHttpContract.GetDeleteSecretFilterUri(resourceId, secretId, secretFilterType);
            return await SendAsync<object, SecretResult>(SecretManagerHttpContract.DeleteSecretFilterMethod, uri, null, logger);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ResourceSecretsResult>> GetSecretsAsync(
            IEnumerable<Guid> resourceIds,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(resourceIds, nameof(resourceIds));
            var uri = SecretManagerHttpContract.GetGetSecretsUri(resourceIds);
            return await SendAsync<object, IEnumerable<ResourceSecretsResult>>(SecretManagerHttpContract.GetSecretsMethod, uri, null, logger);
        }

        /// <inheritdoc/>
        public async Task<SecretResult> UpdateSecretAsync(
            Guid resourceId,
            Guid secretId,
            UpdateSecretBody updateSecretBody,
            IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            Requires.NotEmpty(secretId, nameof(secretId));
            var uri = SecretManagerHttpContract.GetUpdateSecretUri(resourceId, secretId);
            return await SendAsync<UpdateSecretBody, SecretResult>(SecretManagerHttpContract.UpdateSecretMethod, uri, updateSecretBody, logger);
        }
    }
}
