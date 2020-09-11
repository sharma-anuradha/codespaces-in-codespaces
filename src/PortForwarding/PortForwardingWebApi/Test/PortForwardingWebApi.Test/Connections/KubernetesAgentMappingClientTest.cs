using k8s;
using k8s.Models;
using Microsoft.Rest;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Test.Connections
{
    public class KubernetesAgentMappingClientTest
    {
        private readonly IDiagnosticsLogger logger;

        public KubernetesAgentMappingClientTest()
        {
            IDiagnosticsLoggerFactory loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();
        }

        [Fact]
        public async Task CreateAgentConnectionMappingAsync_CreateEndpoint()
        {
            var mockKubernetesClient = CreateMockKubernetesClient();

            var client = new KubernetesAgentMappingClient(MockPortForwardingAppSettings.Settings, mockKubernetesClient.Object);
            var connectionDetails = new ConnectionDetails
            {
                WorkspaceId = "ABCDEF0123456789",
                SourcePort = 8080,
                DestinationPort = 9090,
                AgentName = "test-agent-name",
                AgentUid = "test-agent-uuid"
            };

            await client.CreateAgentConnectionMappingAsync(connectionDetails, logger);

            // Cannot verify on the extension method, but verifying on the underlying method is fine.
            mockKubernetesClient.Verify(c => c.CreateNamespacedEndpointsWithHttpMessagesAsync(
                    It.Is<V1Endpoints>(e =>
                        e.Subsets[0].Ports[0].Port == 9090 &&
                        e.Subsets[0].Ports[0].Name == "http-9090" &&
                        e.Subsets[0].Addresses[0].Ip == "10.0.0.1"
                    ),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task CreateAgentConnectionMappingAsync_CreateHttpsEndpoint()
        {
            var mockKubernetesClient = CreateMockKubernetesClient();

            var client = new KubernetesAgentMappingClient(MockPortForwardingAppSettings.Settings, mockKubernetesClient.Object);
            var connectionDetails = new ConnectionDetails
            {
                WorkspaceId = "ABCDEF0123456789",
                SourcePort = 8080,
                DestinationPort = 9090,
                AgentName = "test-agent-name",
                AgentUid = "test-agent-uuid",
                Hints = new ServerHints { UseHttps = true },
            };

            await client.CreateAgentConnectionMappingAsync(connectionDetails, logger);

            // Cannot verify on the extension method, but verifying on the underlying method is fine.
            mockKubernetesClient.Verify(c => c.CreateNamespacedEndpointsWithHttpMessagesAsync(
                    It.Is<V1Endpoints>(e =>
                        e.Subsets[0].Ports[0].Port == 9090 &&
                        e.Subsets[0].Ports[0].Name == "https-9090" &&
                        e.Subsets[0].Addresses[0].Ip == "10.0.0.1"
                    ),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task CreateAgentConnectionMappingAsync_CreateHttpsIngress()
        {
            var mockKubernetesClient = CreateMockKubernetesClient();

            var mockHttpOperationPod = new Mock<HttpOperationResponse<V1Pod>>();

            var client = new KubernetesAgentMappingClient(MockPortForwardingAppSettings.Settings, mockKubernetesClient.Object);
            var connectionDetails = new ConnectionDetails
            {
                WorkspaceId = "ABCDEF0123456789",
                SourcePort = 8080,
                DestinationPort = 9090,
                AgentName = "test-agent-name",
                AgentUid = "test-agent-uuid",
                Hints = new ServerHints { UseHttps = true },
            };

            await client.CreateAgentConnectionMappingAsync(connectionDetails, logger);

            // Cannot verify on the extension method, but verifying on the underlying method is fine.
            mockKubernetesClient.Verify(c => c.CreateNamespacedIngressWithHttpMessagesAsync(
                    It.Is<Extensionsv1beta1Ingress>(i =>
                        i.Metadata.Annotations["nginx.ingress.kubernetes.io/backend-protocol"] == "HTTPS" &&
                        i.Metadata.Annotations["nginx.ingress.kubernetes.io/configuration-snippet"].Contains("proxy_set_header origin \"https://localhost\"")
                    ),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task CreateAgentConnectionMappingAsync_CreateHttpIngress()
        {
            var mockKubernetesClient = CreateMockKubernetesClient();

            var mockHttpOperationPod = new Mock<HttpOperationResponse<V1Pod>>();

            var client = new KubernetesAgentMappingClient(MockPortForwardingAppSettings.Settings, mockKubernetesClient.Object);
            var connectionDetails = new ConnectionDetails
            {
                WorkspaceId = "ABCDEF0123456789",
                SourcePort = 8080,
                DestinationPort = 9090,
                AgentName = "test-agent-name",
                AgentUid = "test-agent-uuid",
                Hints = new ServerHints { UseHttps = false },
            };

            await client.CreateAgentConnectionMappingAsync(connectionDetails, logger);

            // Cannot verify on the extension method, but verifying on the underlying method is fine.
            mockKubernetesClient.Verify(c => c.CreateNamespacedIngressWithHttpMessagesAsync(
                    It.Is<Extensionsv1beta1Ingress>(i =>
                        !i.Metadata.Annotations.ContainsKey("nginx.ingress.kubernetes.io/backend-protocol") &&
                        i.Metadata.Annotations["nginx.ingress.kubernetes.io/configuration-snippet"].Contains("proxy_set_header origin \"http://localhost\"")
                    ),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task CreateAgentConnectionMappingAsync_CreateForWorkspaceId()
        {
            var mockKubernetesClient = CreateMockKubernetesClient();

            var client = new KubernetesAgentMappingClient(MockPortForwardingAppSettings.Settings, mockKubernetesClient.Object);
            var connectionDetails = new ConnectionDetails
            {
                WorkspaceId = "ABCDEF0123456789",
                SourcePort = 8080,
            };

            await client.CreateAgentConnectionMappingAsync(connectionDetails, logger);

            // Cannot verify on the extension method, but verifying on the underlying method is fine.
            mockKubernetesClient.Verify(c => c.CreateNamespacedIngressWithHttpMessagesAsync(
                    It.Is<Extensionsv1beta1Ingress>(i => i.Spec.Rules[0].Host == "abcdef0123456789-8080.app.vso.io"),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task CreateAgentConnectionMappingAsync_CreateForEnvironmentId()
        {
            var mockKubernetesClient = CreateMockKubernetesClient();

            var client = new KubernetesAgentMappingClient(MockPortForwardingAppSettings.Settings, mockKubernetesClient.Object);
            var connectionDetails = new ConnectionDetails
            {
                WorkspaceId = "ABCDEF0123456789",
                SourcePort = 8080,
                EnvironmentId = "c1fe338d-83cb-4721-ba7f-088efddb7f48"
            };

            await client.CreateAgentConnectionMappingAsync(connectionDetails, logger);

            // Cannot verify on the extension method, but verifying on the underlying method is fine.
            mockKubernetesClient.Verify(c => c.CreateNamespacedIngressWithHttpMessagesAsync(
                    It.Is<Extensionsv1beta1Ingress>(i => i.Spec.Rules[0].Host == "c1fe338d-83cb-4721-ba7f-088efddb7f48-8080.app.vso.io"),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()));
        }

        private Mock<IKubernetes> CreateMockKubernetesClient()
        {
            var mockHttpOperationEndpoint = new Mock<HttpOperationResponse<V1Endpoints>>();
            var mockHttpOperationService = new Mock<HttpOperationResponse<V1Service>>();
            var mockHttpOperationIngress = new Mock<HttpOperationResponse<Extensionsv1beta1Ingress>>();
            var mockKubernetesClient = new Mock<IKubernetes>();
            mockKubernetesClient
                .Setup(m => m.ReadNamespacedPodWithHttpMessagesAsync(
                    It.IsAny<string>(),
                    "default",
                    null,
                    null,
                    null,
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new HttpOperationResponse<V1Pod>
                    {
                        Body = new V1Pod
                        {
                            Status = new V1PodStatus
                            {
                                PodIP = "10.0.0.1",
                            },
                        },
                    });
            mockKubernetesClient
                .Setup(c => c.CreateNamespacedEndpointsWithHttpMessagesAsync(
                    It.IsAny<V1Endpoints>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpOperationEndpoint.Object);
            mockKubernetesClient
                .Setup(c => c.CreateNamespacedServiceWithHttpMessagesAsync(
                    It.IsAny<V1Service>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpOperationService.Object);
            mockKubernetesClient
                .Setup(c => c.CreateNamespacedIngressWithHttpMessagesAsync(
                    It.IsAny<Extensionsv1beta1Ingress>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockHttpOperationIngress.Object);

            return mockKubernetesClient;
        }
    }
}
