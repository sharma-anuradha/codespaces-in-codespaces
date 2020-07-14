// <copyright file="ServiceBusClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus
{
    /// <inheritdoc/>
    public class ServiceBusClientProvider : IServiceBusClientProvider
    {
        private readonly IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusClientProvider"/> class.
        /// </summary>
        /// <param name="controlPlaneAzureResourceAccessor">The control plane azure resource accessor.</param>
        public ServiceBusClientProvider(
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
        {
            Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));
            this.controlPlaneAzureResourceAccessor = controlPlaneAzureResourceAccessor;
        }

        /// <inheritdoc/>
        public async Task<IQueueClient> GetQueueClientAsync(string queueName, IDiagnosticsLogger logger)
        {
            var connectionString = await controlPlaneAzureResourceAccessor.GetStampServiceBusConnectionStringAsync(logger);

            return new QueueClient(connectionString, queueName);
        }

        /// <inheritdoc/>
        public async Task<ISessionClient> GetSessionClientAsync(string queueName, IDiagnosticsLogger logger)
        {
            var connectionString = await controlPlaneAzureResourceAccessor.GetStampServiceBusConnectionStringAsync(logger);

            return new SessionClient(connectionString, queueName);
        }

        /// <inheritdoc/>
        public async Task<IMessageSender> GetMessageSender(string queueName, IDiagnosticsLogger logger)
        {
            var connectionString = await controlPlaneAzureResourceAccessor.GetStampServiceBusConnectionStringAsync(logger);

            return new MessageSender(connectionString, queueName);
        }

        /// <inheritdoc/>
        public async Task<ITopicClient> GetTopicClientAsync(string topicName, IDiagnosticsLogger logger)
        {
            var connectionString = await controlPlaneAzureResourceAccessor.GetStampServiceBusConnectionStringAsync(logger);

            return new TopicClient(connectionString, topicName);
        }

        /// <inheritdoc/>
        public async Task<ISubscriptionClient> GetSubscriptionClientAsync(string topicName, string subscriptionName, IDiagnosticsLogger logger)
        {
            var connectionString = await controlPlaneAzureResourceAccessor.GetStampServiceBusConnectionStringAsync(logger);

            return new SubscriptionClient(connectionString, topicName, subscriptionName);
        }
    }
}
