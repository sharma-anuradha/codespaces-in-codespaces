// <copyright file="ServiceBusSessionClientProviderBase.cs" company="Microsoft">
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
    /// Proxy for <see cref="ISessionClient" /> with initialization set up.
    /// </summary>
    public abstract class ServiceBusSessionClientProviderBase : IAsyncWarmup, ISessionClientProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBusSessionClientProviderBase"/> class.
        /// </summary>
        /// <param name="queueName">The service bus queue name.</param>
        /// <param name="serviceBusClientProvider">The service bus client provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        protected ServiceBusSessionClientProviderBase(
            string queueName,
            IServiceBusClientProvider serviceBusClientProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
        {
            var logger = loggerFactory.New(defaultLogValues);

            Client = new AsyncLazy<ISessionClient>(() => serviceBusClientProvider.GetSessionClientAsync(queueName, logger));
        }

        /// <inheritdoc/>
        public Lazy<Task<ISessionClient>> Client { get; }

        /// <inheritdoc />
        public Task WarmupCompletedAsync() => Client.Value;
    }
}
