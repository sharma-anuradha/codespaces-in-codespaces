using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VsCloudKernel.SignalService.ServiceHubTests;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.RelayServiceHubTests
{
    public class RelayServiceHubTestsBase
    {
        protected static async Task TestInternal(
            Dictionary<string, IClientProxy> clientProxies1,
            Dictionary<string, IClientProxy> clientProxies2,
            RelayService relayService1,
            RelayService relayService2)
        {
            var conn1Proxy = new Dictionary<string, object[]>();
            clientProxies1.Add("conn1", MockUtils.CreateClientProxy((m, _args) =>
            {
                conn1Proxy[m] = _args;
                return Task.CompletedTask;
            }));
            var conn2Proxy = new Dictionary<string, object[]>();
            clientProxies2.Add("conn2", MockUtils.CreateClientProxy((m, _args) =>
            {
                conn2Proxy[m] = _args;
                return Task.CompletedTask;
            }));

            var hubId = await relayService1.CreateHubAsync(null, default);

            await relayService1.JoinHubAsync("conn1", hubId, null, false, default);

            Assert.Empty(conn2Proxy);
            AssertParticpantChanged(conn1Proxy, "conn1", ParticipantChangeType.Added);

            conn1Proxy.Clear();
            conn2Proxy.Clear();

            var relayHubInfo = await relayService2.JoinHubAsync("conn2", hubId, null, true, default);
            Assert.Equal(2, relayHubInfo.Count);
            Assert.True(relayHubInfo.ContainsKey("conn1"));
            Assert.True(relayHubInfo.ContainsKey("conn2"));
            AssertParticpantChanged(conn1Proxy, "conn2", ParticipantChangeType.Added);
            AssertParticpantChanged(conn2Proxy, "conn2", ParticipantChangeType.Added);


            conn1Proxy.Clear();
            conn2Proxy.Clear();
            await relayService1.SendDataHubAsync("conn1", hubId, SendOption.None, null, "type1", Encoding.UTF8.GetBytes("hi"), default);

            AssertDataReceived(conn1Proxy, hubId, "conn1");
            AssertDataReceived(conn2Proxy, hubId, "conn1");

            conn1Proxy.Clear();
            conn2Proxy.Clear();

            await relayService1.UpdateAsync("conn1", hubId, new Dictionary<string, object>() 
            {
                { "prop1", 100 },
            }, default);
            AssertParticpantChanged(conn1Proxy, "conn1", ParticipantChangeType.Updated);
            AssertParticpantChanged(conn2Proxy, "conn1", ParticipantChangeType.Updated);

            conn1Proxy.Clear();
            conn2Proxy.Clear();
            await relayService2.LeaveHubAsync("conn2", hubId, default);
            AssertParticpantChanged(conn1Proxy, "conn2", ParticipantChangeType.Removed);
            AssertParticpantChanged(conn2Proxy, "conn2", ParticipantChangeType.Removed);

            // re-join
            await relayService2.JoinHubAsync("conn2", hubId, null, false, default);

            conn1Proxy.Clear();
            conn2Proxy.Clear();
            await relayService1.DeleteHubAsync(hubId, default);
            AssertDeleted(conn1Proxy, hubId);
            AssertDeleted(conn2Proxy, hubId);
        }

        private static void AssertDataReceived(
            Dictionary<string, object[]> connProxy,
            string hubId,
            string participantId)
        {
            Assert.NotEmpty(connProxy);
            Assert.True(connProxy.ContainsKey(RelayHubMethods.MethodReceiveData));
            var args = connProxy[RelayHubMethods.MethodReceiveData];
            Assert.Equal(hubId, args[0]);
            Assert.Equal(participantId, args[1]);
            Assert.Equal(0, args[2]);
            Assert.Equal("type1", args[3]);
            Assert.Equal("hi", Encoding.UTF8.GetString((byte[])args[4]));
        }

        private static object[] AssertParticpantChanged(
            Dictionary<string, object[]> connProxy,
            string participantId,
            ParticipantChangeType changeType)
        {
            Assert.NotEmpty(connProxy);
            var args = connProxy[RelayHubMethods.MethodParticipantChanged];
            Assert.Equal(participantId, args[1]);
            Assert.Equal(changeType, args[3]);
            return args;
        }

        private static void AssertDeleted(
            Dictionary<string, object[]> connProxy,
            string hubId)
        {
            Assert.NotEmpty(connProxy);
            var args = connProxy[RelayHubMethods.MethodHubDeleted];
            Assert.Equal(hubId, args[0]);
        }
    }
}
