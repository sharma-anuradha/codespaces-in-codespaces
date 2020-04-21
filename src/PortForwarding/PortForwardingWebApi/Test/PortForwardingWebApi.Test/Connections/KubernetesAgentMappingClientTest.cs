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
            var mockHttpOperationService = new Mock<HttpOperationResponse<V1Service>>();
            var mockHttpOperationIngress = new Mock<HttpOperationResponse<Extensionsv1beta1Ingress>>();
            var mockKubernetesClient = new Mock<IKubernetes>();
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
