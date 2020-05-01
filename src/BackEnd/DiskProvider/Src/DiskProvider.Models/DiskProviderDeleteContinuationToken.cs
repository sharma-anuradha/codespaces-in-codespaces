// <copyright file="DiskProviderDeleteContinuationToken.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Models
{
    /// <summary>
    /// Represents the continuation token for the disk delete operation.
    /// </summary>
    public class DiskProviderDeleteContinuationToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiskProviderDeleteContinuationToken"/> class.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info.</param>
        /// <param name="nextState">Next state.</param>
        /// <param name="retryAttempt">Retry attempt.</param>
        public DiskProviderDeleteContinuationToken(
            AzureResourceInfo azureResourceInfo,
            DiskProviderDeleteState nextState,
            int retryAttempt = 0)
        {
            AzureResourceInfo = azureResourceInfo;
            NextState = nextState;
            RetryAttempt = retryAttempt;
        }

        /// <summary>
        /// Gets or sets azure resource info.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets the next state of the operation.
        /// </summary>
        public DiskProviderDeleteState NextState { get; }

        /// <summary>
        /// Gets the retry attempt count.
        /// </summary>
        public int RetryAttempt { get; }
    }
}
