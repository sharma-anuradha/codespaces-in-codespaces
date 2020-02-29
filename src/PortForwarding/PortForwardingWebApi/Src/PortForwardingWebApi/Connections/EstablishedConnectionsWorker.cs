// <copyright file="EstablishedConnectionsWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Connections
{
    /// <summary>
    /// Background Worker for establishing missed connections.
    /// </summary>
    public class EstablishedConnectionsWorker : BackgroundService
    {
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="EstablishedConnectionsWorker"/> class.
        /// </summary>
        /// <param name="queueClientProvider">The service bus queue provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="agentMappingClient">The agent mapping client.</param>
        public EstablishedConnectionsWorker(
                IServiceBusQueueClientProvider queueClientProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                IAgentMappingClient agentMappingClient)
        {
            QueueClientProvider = Requires.NotNull(queueClientProvider, nameof(queueClientProvider));
            LoggerFactory = Requires.NotNull(loggerFactory, nameof(loggerFactory));
            AgentMappingClient = Requires.NotNull(agentMappingClient, nameof(agentMappingClient));
        }

        private IServiceBusQueueClientProvider QueueClientProvider { get; }

        private IDiagnosticsLoggerFactory LoggerFactory { get; }

        private IAgentMappingClient AgentMappingClient { get; }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var logger = LoggerFactory.New();

            var client = await QueueClientProvider.GetQueueClientAsync("connections-established", logger);

            client.RegisterSessionHandler(
                ProcessSessionMessage,
                new SessionHandlerOptions((e) =>
                {
                    logger.LogException("established_connection_worker_handle_session_message_exception", e.Exception);

                    return Task.CompletedTask;
                })
                {
                    MaxConcurrentSessions = 1,
                    AutoComplete = true, // We'll have the relevant information for debugging in logs so we can get rid of the message.
                });
        }

        private Task ProcessSessionMessage(IMessageSession session, Message message, CancellationToken cancellationToken)
        {
            var logger = LoggerFactory.New();

            return logger.OperationScopeAsync(
                "established_connection_worker_process_connection_established",
                async (childLogger) =>
                {
                    var connection = JsonSerializer.Deserialize<ConnectionEstablished>(message.Body, jsonSerializerOptions);
                    try
                    {
                        await AgentMappingClient.CreateAgentConnectionMappingAsync(connection, childLogger);
                    }
                    catch (Exception ex)
                    {
                        childLogger.LogException("established_connection_worker_create_mapping_failed", ex);
                    }
                });
        }
    }
}
