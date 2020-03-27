// <copyright file="IServiceBusQueueClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus
{
    /// <summary>
    /// An Azure Service Bus Queue client provider.
    /// </summary>
    public interface IServiceBusQueueClientProvider
    {
        /// <summary>
        /// Gets the queue client for service bus queue operations.
        /// </summary>
        /// <returns>The queue client instance.</returns>
        Task<IQueueClient> GetQueueClientAsync(string queueName, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the session client for service bus queue operations.
        /// </summary>
        /// <returns>The queue client instance.</returns>
        Task<ISessionClient> GetSessionClientAsync(string queueName, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the sender client for service bus queue operations.
        /// </summary>
        /// <returns>The message sender client instance.</returns>
        Task<IMessageSender> GetMessageSender(string queueName, IDiagnosticsLogger logger);
    }
}