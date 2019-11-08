using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.PresenceServiceHubTests
{
    public class RelayServiceHubTests
    {
        private readonly Dictionary<string, IClientProxy> clientProxies;
        private readonly RelayService relayService;

        public RelayServiceHubTests()
        {
            this.clientProxies = new Dictionary<string, IClientProxy>();
            var serviceLogger = new Mock<ILogger<RelayService>>();

            var trace = new TraceSource("RelayServiceServiceHubTests");
            this.relayService = new RelayService(
                new HubServiceOptions() { Id = "mock" },
                MockUtils.CreateSingleHubContextHostMock<RelayServiceHub>(this.clientProxies),
                serviceLogger.Object);
        }

        [Fact]
        public async Task Test()
        {
            var conn1Proxy = new Dictionary<string, object[]>();
            this.clientProxies.Add("conn1", MockUtils.CreateClientProxy((m, _args) =>
            {
                conn1Proxy[m] = _args;
                return Task.CompletedTask;
            }));
            var conn2Proxy = new Dictionary<string, object[]>();
            this.clientProxies.Add("conn2", MockUtils.CreateClientProxy((m, _args) =>
            {
                conn2Proxy[m] = _args;
                return Task.CompletedTask;
            }));

            var hubId = await this.relayService.CreateHubAsync(null, default);

            await this.relayService.JoinHubAsync("conn1", hubId, null, false, default);
            Assert.Empty(conn2Proxy);
            Assert.True(conn1Proxy.ContainsKey(RelayHubMethods.MethodParticipantChanged));
            var args = conn1Proxy[RelayHubMethods.MethodParticipantChanged];
            Assert.Equal(hubId, args[0]);
            Assert.Equal("conn1", args[1]);
            Assert.Null(args[2]);

            await this.relayService.JoinHubAsync("conn2", hubId, null, false, default);
            Assert.NotEmpty(conn2Proxy);
            args = conn1Proxy[RelayHubMethods.MethodParticipantChanged];
            Assert.Equal("conn2", args[1]);

            conn1Proxy.Clear();
            conn2Proxy.Clear();
            await this.relayService.SendDataHubAsync("conn1", hubId, SendOption.None, null, "type1", Encoding.UTF8.GetBytes("hi"), default);
            Assert.NotEmpty(conn1Proxy);
            Assert.NotEmpty(conn2Proxy);
            Assert.True(conn2Proxy.ContainsKey(RelayHubMethods.MethodReceiveData));
            args = conn2Proxy[RelayHubMethods.MethodReceiveData];
            Assert.Equal(hubId, args[0]);
            Assert.Equal("conn1", args[1]);
            Assert.Equal(0, args[2]);
            Assert.Equal("type1", args[3]);
            Assert.Equal("hi", Encoding.UTF8.GetString((byte[])args[4]));
        }
    }
}
