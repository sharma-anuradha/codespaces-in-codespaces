// <copyright file="IQueueFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// A queue factory contract.
    /// </summary>
    public interface IQueueFactory
    {
        /// <summary>
        /// Get or create a queue instance.
        /// </summary>
        /// <param name="queueId">The queue id to be created/retrieved.</param>
        /// <param name="azureLocation">An optional Azure location.</param>
        /// <returns>The instance of the queue.</returns>
        IQueue GetOrCreate(string queueId, AzureLocation? azureLocation = null);
    }
}
