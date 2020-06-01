using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.ServiceHubTests;
using Moq;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.RelayServiceHubTests
{
    public class RelayServiceHubTests : RelayServiceHubTestsBase
    {
        protected override Dictionary<string, IClientProxy> ClientProxies1 { get; }
        protected override Dictionary<string, IClientProxy> ClientProxies2 { get; }
        protected override RelayService RelayService1 { get; }
        protected override RelayService RelayService2 { get; }

        public RelayServiceHubTests()
        {
            ClientProxies1 = ClientProxies2 = new Dictionary<string, IClientProxy>();
            var serviceLogger = new Mock<ILogger<RelayService>>();
            RelayService1 = RelayService2 = new RelayService(
                new HubServiceOptions() { Id = "mock" },
                MockUtils.CreateSingleHubContextHostMock<RelayServiceHub>(ClientProxies1),
                serviceLogger.Object);
        }
    }
}
