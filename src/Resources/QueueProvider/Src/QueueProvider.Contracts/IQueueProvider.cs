// <copyright file="IQueueProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts
{
    /// <summary>
    /// Interface to manage virtual machine queue.
    /// </summary>
    public interface IQueueProvider : IQueueProviderDepricatedV0
    {
        /// <summary>
        /// Create vm input queue.
        /// </summary>
        /// <param name="input">queue create input.</param>
        /// <param name="logger">logger.</param>
        /// <returns>queue.</returns>
        Task<QueueProviderCreateResult> CreateAsync(
            QueueProviderCreateInput input,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Deletes vm input queue.
        /// </summary>
        /// <param name="input">Queue delete input.</param>
        /// <param name="logger">logger.</param>
        /// <returns>task.</returns>
        Task DeleteAsync(
            QueueProviderDeleteInput input,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Checks if queue exists.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info.</param>
        /// <param name="logger">logger.</param>
        /// <returns>result.</returns>
        Task<object> ExistsAync(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Push message to queue.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info.</param>
        /// <param name="queueMessage">queue message.</param>
        /// <param name="logger">logger.</param>
        /// <returns>task.</returns>
        Task<QueueProviderPushResult> PushMessageAsync(
            AzureResourceInfo azureResourceInfo,
            QueueMessage queueMessage,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets queue connection info.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>Queue connection info.</returns>
        Task<QueueConnectionInfo> GetQueueConnectionInfoAsync(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets details about the queue.
        /// </summary>
        /// <param name="input">Queue provider get details input.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>Queue details info.</returns>
        /// <remarks>This is interim code, can go away when all environments track queue as a resource component in the record.</remarks>
        Task<QueueProviderGetDetailsResult> GetDetailsAsync(
            QueueProviderGetDetailsInput input,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Clears the queue of old messages.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>Task.</returns>
        Task ClearQueueAsync(AzureResourceInfo azureResourceInfo, IDiagnosticsLogger logger);
    }
}