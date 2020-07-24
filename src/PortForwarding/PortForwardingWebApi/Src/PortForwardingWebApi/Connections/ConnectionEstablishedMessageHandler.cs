// <copyright file="ConnectionEstablishedMessageHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Connections
{
    /// <summary>
    /// "connection-established" queue message handler.
    /// </summary>
    public class ConnectionEstablishedMessageHandler : IConnectionEstablishedMessageHandler
    {
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionEstablishedMessageHandler"/> class.
        /// </summary>
        /// <param name="agentMappingClient">The agent mapping client.</param>
        public ConnectionEstablishedMessageHandler(
            IAgentMappingClient agentMappingClient)
        {
            AgentMappingClient = Requires.NotNull(agentMappingClient, nameof(agentMappingClient));
        }

        private IAgentMappingClient AgentMappingClient { get; }

        /// <inheritdoc/>
        public async Task ProcessSessionMessageAsync(
            Message message,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken)
        {
            var connection = JsonSerializer.Deserialize<ConnectionDetails>(message.Body, jsonSerializerOptions);

            logger.AddConnectionDetails(connection);

            var registration = new AgentRegistration
            {
                Name = connection.AgentName,
                Uid = connection.AgentUid,
            };
            await AgentMappingClient.RegisterAgentAsync(registration, logger);

            await AgentMappingClient.CreateAgentConnectionMappingAsync(connection, logger);

            await AgentMappingClient.RemoveBusyAgentFromDeploymentAsync(connection.AgentName, logger);
        }
    }
}