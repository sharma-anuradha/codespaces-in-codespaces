// <copyright file="IQueueFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
        /// <returns>The instance of the queue.</returns>
        IQueue GetOrCreate(string queueId);
    }
}
