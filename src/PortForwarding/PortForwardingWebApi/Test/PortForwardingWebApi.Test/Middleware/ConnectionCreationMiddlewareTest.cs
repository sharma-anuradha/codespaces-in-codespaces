// <copyright file="ConnectionCreationMiddlewareTest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using k8s.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.ServiceBus;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Middleware;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Test.Middleware
{
    public class ConnectionCreationMiddlewareTest
    {
        private readonly ConnectionCreationMiddleware middleware;
        private readonly IDiagnosticsLogger logger;
        private readonly PortForwardingHostUtils hostUtils;

        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public ConnectionCreationMiddlewareTest()
        {
            IDiagnosticsLoggerFactory loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();
            hostUtils = new PortForwardingHostUtils(MockPortForwardingAppSettings.Settings.HostsConfigs);

            middleware = new ConnectionCreationMiddleware(context => Task.CompletedTask);
        }

        [Fact]
        public async Task InvokeAsync_NoToken_401()
        {
            var queueClientProvider = new Mock<IServiceBusQueueClientProvider>();
            var mappingClient = new Mock<IAgentMappingClient>();
            var context = CreateMockContext(isAuthenticated: false);

            await middleware.InvokeAsync(
                context,
                logger,
                queueClientProvider.Object,
                mappingClient.Object,
                hostUtils,
                MockPortForwardingAppSettings.Settings);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_ValidConfiguration_HeaderDetails_SendMessage()
        {
            var queueClientProvider = new Mock<IServiceBusQueueClientProvider>();
            var mappingClient = new Mock<IAgentMappingClient>();
            var context = CreateMockContext();

            var queueClient = new Mock<IQueueClient>();
            queueClientProvider
                .Setup(provider => provider.GetQueueClientAsync(QueueNames.NewConnections, It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(queueClient.Object);

            mappingClient
                .Setup(c => c.WaitForEndpointReadyAsync(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(MockKubernetesObjects.Endpoint);

            await middleware.InvokeAsync(
                context,
                logger,
                queueClientProvider.Object,
                mappingClient.Object,
                hostUtils,
                MockPortForwardingAppSettings.Settings);

            var connectionRequest = new ConnectionRequest
            {
                WorkspaceId = "a68c43fa9e015e45e046c85d502ec5e4b774",
                Port = 8080,
                Token = "super_secret_token",
                VSLiveShareApiEndpoint = MockPortForwardingAppSettings.Settings.VSLiveShareApiEndpoint,
            };
            Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
            Assert.Equal(
                "https://0.0.0.0/test/path",
                context.Response.Headers.GetValueOrDefault("Location"));

            queueClient.Verify(c => c.SendAsync(It.Is<Message>(msg =>
                msg.SessionId == connectionRequest.WorkspaceId &&
                // Deserialize to string for readable assertion errors.
                Encoding.UTF8.GetString(msg.Body) == JsonSerializer.Serialize(connectionRequest, jsonSerializerOptions)
            )));
        }

        [Fact]
        public async Task InvokeAsync_InvalidConfiguration_HeaderDetails_SendMessage()
        {
            var queueClientProvider = new Mock<IServiceBusQueueClientProvider>();
            var mappingClient = new Mock<IAgentMappingClient>();
            var context = CreateMockContext(invalidHeaders: true);

            var queueClient = new Mock<IQueueClient>();
            queueClientProvider
                .Setup(provider => provider.GetQueueClientAsync(QueueNames.NewConnections, It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(queueClient.Object);

            mappingClient
                .Setup(c => c.WaitForEndpointReadyAsync(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(MockKubernetesObjects.Endpoint);

            await middleware.InvokeAsync(
                context,
                logger,
                queueClientProvider.Object,
                mappingClient.Object,
                hostUtils,
                MockPortForwardingAppSettings.Settings);

            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_ValidConfiguration_SendsMessage()
        {
            var queueClientProvider = new Mock<IServiceBusQueueClientProvider>();
            var mappingClient = new Mock<IAgentMappingClient>();
            var context = CreateMockContext();

            var queueClient = new Mock<IQueueClient>();
            queueClientProvider
                .Setup(provider => provider.GetQueueClientAsync(QueueNames.NewConnections, It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(queueClient.Object);

            mappingClient
                .Setup(c => c.WaitForEndpointReadyAsync(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(MockKubernetesObjects.Endpoint);

            await middleware.InvokeAsync(
                context,
                logger,
                queueClientProvider.Object,
                mappingClient.Object,
                hostUtils,
                MockPortForwardingAppSettings.Settings);

            var connectionRequest = new ConnectionRequest
            {
                WorkspaceId = "a68c43fa9e015e45e046c85d502ec5e4b774",
                Port = 8080,
                Token = "super_secret_token",
                VSLiveShareApiEndpoint = MockPortForwardingAppSettings.Settings.VSLiveShareApiEndpoint,
            };
            Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
            Assert.Equal(
                "https://0.0.0.0/test/path",
                context.Response.Headers.GetValueOrDefault("Location"));

            queueClient.Verify(c => c.SendAsync(It.Is<Message>(msg =>
                msg.SessionId == connectionRequest.WorkspaceId &&
                // Deserialize to string for readable assertion errors.
                Encoding.UTF8.GetString(msg.Body) == JsonSerializer.Serialize(connectionRequest, jsonSerializerOptions)
            )));
        }

        [Fact]
        public async Task InvokeAsync_ValidConfiguration_ServiceWaitTimesOut()
        {
            var queueClientProvider = new Mock<IServiceBusQueueClientProvider>();
            var queueClient = new Mock<IQueueClient>();
            queueClientProvider
                .Setup(provider => provider.GetQueueClientAsync(QueueNames.NewConnections, It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(queueClient.Object);

            var mappingClient = new Mock<IAgentMappingClient>();
            mappingClient
                .Setup(i => i.WaitForEndpointReadyAsync(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .ThrowsAsync(new TaskCanceledException());

            var context = CreateMockContext();

            await middleware.InvokeAsync(
                context,
                logger,
                queueClientProvider.Object,
                mappingClient.Object,
                hostUtils,
                MockPortForwardingAppSettings.Settings);

            Assert.Equal(StatusCodes.Status504GatewayTimeout, context.Response.StatusCode);
        }

        private HttpContext CreateMockContext(bool invalidHeaders = false, bool isAuthenticated = true)
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    Method = "GET",
                    Scheme = "https",
                    Host = new HostString("testhost"),
                    PathBase = new PathString(string.Empty),
                    Path = new PathString("/test/path"),
                    QueryString = new QueryString(string.Empty)
                }
            };

            if (!invalidHeaders)
            {
                context.Request.Headers.Add(PortForwardingHeaders.WorkspaceId, "a68c43fa9e015e45e046c85d502ec5e4b774");
                context.Request.Headers.Add(PortForwardingHeaders.Port, "8080");
            }

            if (isAuthenticated)
            {
                context.Request.Headers.Add(PortForwardingHeaders.Token, "super_secret_token");
            }

            return context;
        }
    }
}