// <copyright file="ServiceBusQueueClientProviderBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
        /// <param name="serviceBusClientProvider">The service bus client provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        protected ServiceBusQueueClientProviderBase(
            string queueName,
            IServiceBusClientProvider serviceBusClientProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
        {
            var logger = loggerFactory.New(defaultLogValues);

            Client = new AsyncLazy<IQueueClient>(() => serviceBusClientProvider.GetQueueClientAsync(queueName, logger));
        }

        /// <inheritdoc/>
        public Lazy<Task<IQueueClient>> Client { get; }

        /// <inheritdoc />
        public Task WarmupCompletedAsync() => Client.Value;
    }
}
