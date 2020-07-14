// <copyright file="EstablishedConnectionsWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Connections
{
    /// <summary>
    /// Background Worker for establishing missed connections.
    /// </summary>
    public class EstablishedConnectionsWorker : BackgroundService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EstablishedConnectionsWorker"/> class.
        /// </summary>
        /// <param name="queueClientProvider">The service bus queue provider.</param>
        /// <param name="messageHandler">The message handler.</param>
        /// <param name="hostApplicationLifetime">The host application lifetime object.</param>
        /// <param name="logger">The logger.</param>
        public EstablishedConnectionsWorker(
            IServiceBusClientProvider queueClientProvider,
            IConnectionEstablishedMessageHandler messageHandler,
            IHostApplicationLifetime hostApplicationLifetime,
            IDiagnosticsLogger logger)
        {
            QueueClientProvider = Requires.NotNull(queueClientProvider, nameof(queueClientProvider));
            MessageHandler = Requires.NotNull(messageHandler, nameof(messageHandler));
            HostApplicationLifetime = Requires.NotNull(hostApplicationLifetime, nameof(hostApplicationLifetime));
            Logger = Requires.NotNull(logger, nameof(logger));
        }

        private IServiceBusClientProvider QueueClientProvider { get; }

        private IConnectionEstablishedMessageHandler MessageHandler { get; }

        private IHostApplicationLifetime HostApplicationLifetime { get; }

        private IDiagnosticsLogger Logger { get; }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var client = await QueueClientProvider.GetQueueClientAsync(QueueNames.EstablishedConnections, Logger);

                client.RegisterSessionHandler(
                    ProcessSessionMessage,
                    new SessionHandlerOptions((e) =>
                    {
                        Logger.LogException("established_connection_worker_handle_session_message_exception", e.Exception);

                        return Task.CompletedTask;
                    })
                    {
                        AutoComplete = true, // We'll have the relevant information for debugging in logs so we can get rid of the message.
                    });
            }
            catch (Exception ex)
            {
                Logger.LogException("established_connection_worker_register_message_handler_failed", ex);
                HostApplicationLifetime.StopApplication();
                throw;
            }
        }

        private Task ProcessSessionMessage(
            IMessageSession session,
            Message message,
            CancellationToken cancellationToken)
        {
            return Logger.OperationScopeAsync(
                "established_connection_worker_process_connection_established",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("CorrelationId", message.CorrelationId);
                    await MessageHandler.ProcessSessionMessageAsync(message, childLogger, cancellationToken);
                },
                swallowException: true);
        }
    }
}