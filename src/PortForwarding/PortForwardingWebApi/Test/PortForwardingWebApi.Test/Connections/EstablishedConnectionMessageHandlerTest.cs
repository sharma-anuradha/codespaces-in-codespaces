using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Connections;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Test.Connections
{
    public class EstablishedConnectionMessageHandlerTest
    {
        private readonly IDiagnosticsLogger logger;

        public EstablishedConnectionMessageHandlerTest()
        {
            IDiagnosticsLoggerFactory loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();
        }

        [Fact]
        public async Task ProcessSessionMessageAsync_CreatesAgentConnectionMapping()
        {
            var mappingClient = new Mock<IAgentMappingClient>();

            var messageHandler = new ConnectionEstablishedMessageHandler(mappingClient.Object);
            var connectionInfo = new ConnectionDetails
            {
                WorkspaceId = "W1",
                SourcePort = 80,
                DestinationPort = 8080,
                AgentName = "Agent007",
                AgentUid = "random-id",
            };
            var message = new Message(JsonSerializer.SerializeToUtf8Bytes(
                connectionInfo,
                new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase}))
            {
                SessionId = connectionInfo.GetMessagingSessionId(),
                Label = MessageLabels.ConnectionEstablished,
            };

            await messageHandler.ProcessSessionMessageAsync(message,
                logger, CancellationToken.None);

            mappingClient.Verify(client =>
                client.CreateAgentConnectionMappingAsync(
                    It.IsAny<ConnectionDetails>(),
                    It.IsAny<IDiagnosticsLogger>()));
        }

        [Fact]
        public async Task ProcessSessionMessageAsync_RegistersTheAgentPod()
        {
            var mappingClient = new Mock<IAgentMappingClient>();

            var messageHandler = new ConnectionEstablishedMessageHandler(mappingClient.Object);
            var connectionInfo = new ConnectionDetails
            {
                WorkspaceId = "W1",
                SourcePort = 80,
                DestinationPort = 8080,
                AgentName = "Agent007",
                AgentUid = "random-id",
            };
            var message = new Message(JsonSerializer.SerializeToUtf8Bytes(
                connectionInfo,
                new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase}))
            {
                SessionId = connectionInfo.GetMessagingSessionId(),
                Label = MessageLabels.ConnectionEstablished,
            };

            await messageHandler.ProcessSessionMessageAsync(message,
                logger, CancellationToken.None);

            mappingClient.Verify(client =>
                client.RegisterAgentAsync(
                    It.Is<AgentRegistration>(
                        a => a.Name == connectionInfo.AgentName && a.Uid == connectionInfo.AgentUid),
                    It.IsAny<IDiagnosticsLogger>()));
        }

        [Fact]
        public async Task ProcessSessionMessageAsync_RemovesAgentFromPool()
        {
            var mappingClient = new Mock<IAgentMappingClient>();

            var messageHandler = new ConnectionEstablishedMessageHandler(mappingClient.Object);
            var connectionInfo = new ConnectionDetails
            {
                WorkspaceId = "W1",
                SourcePort = 80,
                DestinationPort = 8080,
                AgentName = "Agent007",
                AgentUid = "random-id",
            };
            var message = new Message(JsonSerializer.SerializeToUtf8Bytes(
                connectionInfo,
                new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase}))
            {
                SessionId = connectionInfo.GetMessagingSessionId(),
                Label = MessageLabels.ConnectionEstablished,
            };

            await messageHandler.ProcessSessionMessageAsync(message,
                logger, CancellationToken.None);

            mappingClient.Verify(client =>
                client.RemoveBusyAgentFromDeploymentAsync("Agent007", It.IsAny<IDiagnosticsLogger>()));
        }
    }
}