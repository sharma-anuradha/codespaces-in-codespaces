// <copyright file="IQueueProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Abstractions
{
    /// <summary>
    /// Interface to manage virtual machine queue.
    /// </summary>
    public interface IQueueProvider
    {
        /// <summary>
        /// Create vm input queue.
        /// </summary>
        /// <param name="input">vm input parameters.</param>
        /// <param name="logger">logger.</param>
        /// <returns>queue.</returns>
        Task<QueueConnectionInfo> CreateQueue(
            QueueProviderCreateInput input,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Deletes vm input queue.
        /// </summary>
        /// <param name="location">queue location.</param>
        /// <param name="queueName">queue name.</param>
        /// <param name="logger">logger.</param>
        /// <returns>task.</returns>
        Task DeleteQueueAsync(
            AzureLocation location,
            string queueName,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Checks if queue exists.
        /// </summary>
        /// <param name="location">queue locaion.</param>
        /// <param name="queueName">queue name.</param>
        /// <param name="logger">logger.</param>
        /// <returns>result.</returns>
        Task<object> QueueExistsAync(AzureLocation location, string queueName, IDiagnosticsLogger logger);

        /// <summary>
        /// Push message to queue.
        /// </summary>
        /// <param name="azureVmLocation">location.</param>
        /// <param name="queueName">queue name.</param>
        /// <param name="queueMessage">queue message.</param>
        /// <param name="logger">logger.</param>
        /// <returns>task.</returns>
        Task PushMessageAsync(
            AzureLocation azureVmLocation,
            string queueName,
            QueueMessage queueMessage,
            IDiagnosticsLogger logger);
    }
}