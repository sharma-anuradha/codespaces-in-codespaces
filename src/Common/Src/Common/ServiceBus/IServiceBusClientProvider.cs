// <copyright file="IServiceBusClientProvider.cs" company="Microsoft">
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
    public interface IServiceBusClientProvider
    {
        /// <summary>
        /// Gets the queue client for service bus queue operations.
        /// </summary>
        /// <param name="queueName">The service bus queue name.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The queue client instance.</returns>
        Task<IQueueClient> GetQueueClientAsync(string queueName, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the session client for service bus queue session operations.
        /// </summary>
        /// <param name="queueName">The service bus queue name.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The queue client instance.</returns>
        Task<ISessionClient> GetSessionClientAsync(string queueName, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the topic client for service bus topic operations.
        /// </summary>
        /// <param name="topicName">The service bus topic name.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The queue client instance.</returns>
        Task<ITopicClient> GetTopicClientAsync(string topicName, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the subscription client for service bus subscription operations.
        /// </summary>
        /// <param name="topicName">The service bus topic name.</param>
        /// <param name="subscriptionName">The service bus subscription name.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The queue client instance.</returns>
        Task<ISubscriptionClient> GetSubscriptionClientAsync(string topicName, string subscriptionName, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the sender client for service bus queue operations.
        /// </summary>
        /// <param name="queueName">The service bus queue name.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The message sender client instance.</returns>
        Task<IMessageSender> GetMessageSender(string queueName, IDiagnosticsLogger logger);
    }
}
