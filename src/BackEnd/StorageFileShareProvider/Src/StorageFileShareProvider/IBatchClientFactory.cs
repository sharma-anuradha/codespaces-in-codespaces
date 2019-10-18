// <copyright file="IBatchClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
{
    /// <summary>
    /// Batch client factory.
    /// </summary>
    public interface IBatchClientFactory
    {
        /// <summary>
        /// Get a batch client given a location.
        /// </summary>
        /// <param name="location">An azure location.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>Batch client.</returns>
        Task<BatchClient> GetBatchClient(string location, IDiagnosticsLogger logger);

        /// <summary>
        /// Get a batch client given a location.
        /// </summary>
        /// <param name="location">An azure location.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>Batch client.</returns>
        Task<BatchClient> GetBatchClient(AzureLocation location, IDiagnosticsLogger logger);
    }
}
