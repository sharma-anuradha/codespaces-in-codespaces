// <copyright file="BatchClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
{
    /// <summary>
    /// Implementation of <see cref="IBatchClientFactory"/>.
    /// </summary>
    public class BatchClientFactory : IBatchClientFactory
    {
        private readonly IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchClientFactory"/> class.
        /// </summary>
        /// <param name="controlPlaneAzureResourceAccessor">An implementation of the <see cref="IControlPlaneAzureResourceAccessor"/> interface.</param>
        public BatchClientFactory(IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
        {
            this.controlPlaneAzureResourceAccessor = Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));
        }

        /// <inheritdoc/>
        public async Task<BatchClient> GetBatchClient(string location, IDiagnosticsLogger logger)
        {
            if (!Enum.TryParse(location, true, out AzureLocation azureLocation))
            {
                throw new NotSupportedException($"Location of {location} is not supported");
            }

            return await GetBatchClient(azureLocation, logger);
        }

        /// <inheritdoc/>
        public async Task<BatchClient> GetBatchClient(AzureLocation azureLocation, IDiagnosticsLogger logger)
        {
            var (accountName, accountKey, accountEndpoint) = await controlPlaneAzureResourceAccessor.GetStampBatchAccountAsync(
                azureLocation,
                logger.WithValues(new LogValueSet()));
            var credentials = new BatchSharedKeyCredentials(
                accountEndpoint,
                accountName,
                accountKey);
            return BatchClient.Open(credentials);
        }
    }
}
