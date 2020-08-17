using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Clients;
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
            var newConnectionsQueueClientProvider = new Mock<IEstablishedConnectionsQueueClientProvider>();
            var queueClient = new Mock<IQueueClient>();
            newConnectionsQueueClientProvider
                .SetupGet(provider => provider.Client)
                .Returns(new AsyncLazy<IQueueClient>(() => queueClient.Object));

            var messageHandler = new Mock<IConnectionEstablishedMessageHandler>();
            var hostApplicationLifetime = new Mock<IHostApplicationLifetime>();

            var worker = new EstablishedConnectionsWorker(
                newConnectionsQueueClientProvider.Object,
                messageHandler.Object,
                hostApplicationLifetime.Object,
                logger);

            await worker.StartAsync(CancellationToken.None);
        }
    }
}
