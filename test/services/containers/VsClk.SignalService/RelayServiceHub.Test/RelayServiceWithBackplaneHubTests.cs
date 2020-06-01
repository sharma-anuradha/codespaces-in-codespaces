
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.ServiceHubTests;
using Moq;

namespace Microsoft.VsCloudKernel.SignalService.RelayServiceHubTests
{
    public class RelayServiceWithBackplaneHubTests : RelayServiceHubTestsBase
    {
        protected override Dictionary<string, IClientProxy> ClientProxies1 { get; }
        protected override Dictionary<string, IClientProxy> ClientProxies2 { get; }
        protected override RelayService RelayService1 { get; }
        protected override RelayService RelayService2 { get; }

        public RelayServiceWithBackplaneHubTests()
        {
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

            var mockBackplaneProvider = new MockBackplaneProvider();
            RelayService1.BackplaneManager.RegisterProvider(mockBackplaneProvider);
            RelayService2.BackplaneManager.RegisterProvider(mockBackplaneProvider);
        }
    }
}
