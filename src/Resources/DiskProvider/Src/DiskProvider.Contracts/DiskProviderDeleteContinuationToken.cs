// <copyright file="DiskProviderDeleteContinuationToken.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Contracts
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
        /// <param name="queueAzureResourceInfo">Queue azure resource info.</param>
        /// <param name="nextState">Next state.</param>
        /// <param name="retryAttempt">Retry attempt.</param>
        public DiskProviderDeleteContinuationToken(
            AzureResourceInfo azureResourceInfo,
            AzureResourceInfo queueAzureResourceInfo,
            DiskProviderDeleteState nextState,
            int retryAttempt = 0)
        {
            AzureResourceInfo = azureResourceInfo;
            QueueAzureResourceInfo = queueAzureResourceInfo;
            NextState = nextState;
            RetryAttempt = retryAttempt;
        }

        /// <summary>
        /// Gets or sets azure resource info.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the queue azure resource info.
        /// </summary>
        public AzureResourceInfo QueueAzureResourceInfo { get; set; }

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
