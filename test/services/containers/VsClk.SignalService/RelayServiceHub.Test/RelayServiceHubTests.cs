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
        private readonly Dictionary<string, IClientProxy> clientProxies;
        private readonly RelayService relayService;

        public RelayServiceHubTests()
        {
            this.clientProxies = new Dictionary<string, IClientProxy>();
            var serviceLogger = new Mock<ILogger<RelayService>>();
            this.relayService = new RelayService(
                new HubServiceOptions() { Id = "mock" },
                MockUtils.CreateSingleHubContextHostMock<RelayServiceHub>(this.clientProxies),
                serviceLogger.Object);
        }

        [Fact]
        public async Task Test()
        {
            await TestInternal(this.clientProxies, this.clientProxies, this.relayService, this.relayService);
        }
    }
}
