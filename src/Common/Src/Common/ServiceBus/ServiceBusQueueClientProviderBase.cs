// <copyright file="ServiceBusQueueClientProviderBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus
{
    /// <summary>
    /// Proxy for <see cref="IQueueClient" /> with initialization set up.
    /// </summary>
    public abstract class ServiceBusQueueClientProviderBase : IAsyncWarmup, IQueueClientProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusQueueClientProviderBase"/> class.
        /// </summary>
        /// <param name="queueName">The service bus queue name.</param>
        /// <param name="requiresSessions">The flag to determine if queue requires sessions.</param>
        /// <param name="serviceBusClientProvider">The service bus client provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        protected ServiceBusQueueClientProviderBase(
            string queueName,
            bool requiresSessions,
            IServiceBusClientProvider serviceBusClientProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
        {
            var logger = loggerFactory.New(defaultLogValues);

            Client = new AsyncLazy<IQueueClient>(async () =>
            {
                var client = await serviceBusClientProvider.GetQueueClientAsync(queueName, logger);

                // We send initial message through the client to warm it up.
                // Next message that passes through the same client is orders of magnitude faster.
                // This is very useful when dealing with Port forwarding agents as the amount of sent messages is low,
                // but the latency needs to be low too.
                var pingMessage = new Message(Encoding.UTF8.GetBytes(WarmupMessageLabel))
                {
                    Label = WarmupMessageLabel,
                    SessionId = requiresSessions ? WarmupMessageLabel : null,
                };
                await client.SendAsync(pingMessage);

                return client;
            });
        }

        /// <summary>
        /// Gets the warmup message label so clients can throw it away.
        /// </summary>
        public string WarmupMessageLabel => "PING";

        /// <inheritdoc/>
        public Lazy<Task<IQueueClient>> Client { get; }

        /// <inheritdoc />
        public Task WarmupCompletedAsync() => Client.Value;
    }
}
