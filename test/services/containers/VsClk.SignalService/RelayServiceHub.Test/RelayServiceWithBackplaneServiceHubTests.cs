
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.BackplaneService;
using Microsoft.VsCloudKernel.SignalService.ServiceHubTests;
using Moq;

namespace Microsoft.VsCloudKernel.SignalService.RelayServiceHubTests
{
    public class RelayServiceWithBackplaneServiceHubTests : RelayServiceHubTestsBase
    {
        protected override Dictionary<string, IClientProxy> ClientProxies1 { get; }
        protected override Dictionary<string, IClientProxy> ClientProxies2 { get; }
        protected override RelayService RelayService1 { get; }
        protected override RelayService RelayService2 { get; }

        public RelayServiceWithBackplaneServiceHubTests()
        {
            var relayBackplaneManager = new RelayBackplaneManager(new Mock<ILogger<RelayBackplaneManager>>().Object);
            var relayBackplaneService = new RelayBackplaneService(
                new IRelayBackplaneServiceNotification[] { },
                new Mock<ILogger<RelayBackplaneService>>().Object,
                relayBackplaneManager);

            var jsonRpcRelaySessionFactory = new JsonRpcRelaySessionFactory(
                relayBackplaneService,
                new Mock<ILogger<JsonRpcRelaySessionFactory>>().Object);
            relayBackplaneService.AddBackplaneServiceNotification(jsonRpcRelaySessionFactory);

            Func<string, IRelayBackplaneProvider> createRelayBackplaneProvider = (serviceId) =>
            {
                var jsonRpcConnectorProvider = new JsonRpcConnectorProvider("localhost", 0, true, new Mock<ILogger<JsonRpcConnectorProvider>>().Object);
                var relayBackplaneServiceProvider = new RelayBackplaneServiceProvider(
                    jsonRpcConnectorProvider,
                    serviceId,
                    new Mock<ILogger<RelayBackplaneServiceProvider>>().Object,
                    default);
                var (serverStream, clientStream) = Nerdbank.Streams.FullDuplexStream.CreatePair();
                jsonRpcConnectorProvider.Attach(JsonRpcConnectorProvider.CreateJsonRpcWithMessagePack(clientStream));
                
                var jsonRpcServer = JsonRpcConnectorProvider.CreateJsonRpcWithMessagePack(serverStream);
                jsonRpcServer.AllowModificationWhileListening = true;
                jsonRpcServer.StartListening();

                jsonRpcRelaySessionFactory.StartRpcSession(jsonRpcServer, serviceId);
                return relayBackplaneServiceProvider;
            };

            ClientProxies1 = new Dictionary<string, IClientProxy>();
            ClientProxies2 = new Dictionary<string, IClientProxy>();
            var serviceLogger = new Mock<ILogger<RelayService>>();
            var backplaneServiceManagerLogger = new Mock<ILogger<RelayBackplaneManager>>();

            RelayService1 = new RelayService(
                new HubServiceOptions() { Id = "mock1" },
                MockUtils.CreateSingleHubContextHostMock<RelayServiceHub>(ClientProxies1),
                serviceLogger.Object,
                new RelayBackplaneManager(backplaneServiceManagerLogger.Object));
            RelayService2 = new RelayService(
                new HubServiceOptions() { Id = "mock2" },
                MockUtils.CreateSingleHubContextHostMock<RelayServiceHub>(ClientProxies2),
                serviceLogger.Object,
                new RelayBackplaneManager(backplaneServiceManagerLogger.Object));

            RelayService1.BackplaneManager.RegisterProvider(createRelayBackplaneProvider("mock1"));
            RelayService2.BackplaneManager.RegisterProvider(createRelayBackplaneProvider("mock2"));
        }
    }
}
