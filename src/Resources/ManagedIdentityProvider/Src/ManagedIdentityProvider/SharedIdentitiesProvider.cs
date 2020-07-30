// <copyright file="SharedIdentitiesProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Identity.Client;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// Client for managing shared identities for CMK.
    /// </summary>
    public class SharedIdentitiesProvider : HttpClientBase, ISharedIdentitiesProvider
    {
        private const string ApiVersion = "2019-06-01";

        private const string LogBaseName = "shared_identity_provider";

        private readonly IControlPlaneAzureResourceAccessor resourceAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedIdentitiesProvider"/> class.
        /// </summary>
        /// <param name="httpClientProvider">HTTP Client Provider.</param>
        /// <param name="resourceAccessor">The Control Plane Resource Accessor.</param>
        /// <param name="logger">Logger.</param>
        public SharedIdentitiesProvider(
            ISharedIdentityHttpClientProvider httpClientProvider,
            IControlPlaneAzureResourceAccessor resourceAccessor)
            : base(httpClientProvider)
        {
            this.resourceAccessor = resourceAccessor;
        }

        /// <inheritdoc/>
        public Task AssignSharedIdentityAsync(
            string storageAccountResourceId,
            StorageSharedIdentity managedIdentity,
            IDiagnosticsLogger logger)
        {
            if (string.IsNullOrEmpty(storageAccountResourceId))
            {
                throw new ArgumentNullException("storageAccountResourceId");
            }

            if (managedIdentity == null)
            {
                throw new ArgumentNullException("managedIdentity");
            }

            return logger.OperationScopeAsync(
                $"{LogBaseName}_assign",
                async (childLogger) =>
                {
                    var sharedIdentityUri = await GetSharedIdentitiesUriAsync(storageAccountResourceId);

                    await SendRawAsync(HttpMethod.Put, sharedIdentityUri, managedIdentity, childLogger);
                });
        }

        /// <inheritdoc/>
        public Task<StorageSharedIdentity> GetSharedIdentityAsync(
            string storageAccountResourceId,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(storageAccountResourceId))
            {
                throw new ArgumentNullException("storageAccountResourceId");
            }

            return logger.OperationScopeAsync(
            $"{LogBaseName}_get",
            async (childLogger) =>
            {
                var sharedIdentityUri = await GetSharedIdentitiesUriAsync(storageAccountResourceId);

                return await SendAsync<object, StorageSharedIdentity>(HttpMethod.Get, sharedIdentityUri, null, childLogger);
            });
        }

        /// <summary>
        /// Builds the URL for an Azure Storage Shared Identities resource.
        /// </summary>
        /// <param name="storageAccountResourceId">The storage account resource ID.</param>
        /// <returns>Uri.</returns>
        public async Task<string> GetSharedIdentitiesUriAsync(string storageAccountResourceId)
        {
            var credentials = await resourceAccessor.GetAzureCredentialsAsync();

            return new Uri(
                new Uri(credentials.Environment.ResourceManagerEndpoint),
                $"{storageAccountResourceId}/sharedIdentities/default?api-version={ApiVersion}")
                .ToString();
        }
    }
}
