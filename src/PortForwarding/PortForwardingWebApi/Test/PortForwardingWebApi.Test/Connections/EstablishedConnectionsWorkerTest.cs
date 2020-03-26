using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Connections;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Test.Connections
{
    public class EstablishedConnectionsWorkerTest
    {
        private readonly IDiagnosticsLogger logger;

        public EstablishedConnectionsWorkerTest()
        {
            IDiagnosticsLoggerFactory loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();
        }

        [Fact]
        public async Task ExecuteAsync_Succeeds()
        {
            var queueClientProvider = new Mock<IServiceBusQueueClientProvider>();
            var queueClient = new Mock<IQueueClient>();
            queueClientProvider
                .Setup(provider => provider.GetQueueClientAsync(QueueNames.EstablishedConnections, It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(queueClient.Object);
            var messageHandler = new Mock<IConnectionEstablishedMessageHandler>();

            var worker = new EstablishedConnectionsWorker(
                queueClientProvider.Object,
                messageHandler.Object,
                logger);

            await worker.StartAsync(CancellationToken.None);
        }
    }
}