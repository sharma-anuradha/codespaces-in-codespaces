using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VsCloudKernel.SignalService.ServiceHubTests;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.RelayServiceHubTests
{
    public abstract class RelayServiceHubTestsBase
    {
        protected abstract Dictionary<string, IClientProxy> ClientProxies1 { get; }
        protected abstract Dictionary<string, IClientProxy> ClientProxies2 { get; }
        protected abstract RelayService RelayService1 { get; }
        protected abstract RelayService RelayService2 { get; }

        [Fact]
        public async Task Test()
        {
            var conn1Proxy = new Dictionary<string, object[]>();
            ClientProxies1.Add("conn1", MockUtils.CreateClientProxy((m, _args) =>
            {
                conn1Proxy[m] = _args;
                return Task.CompletedTask;
            }));
            var conn2Proxy = new Dictionary<string, object[]>();
            ClientProxies2.Add("conn2", MockUtils.CreateClientProxy((m, _args) =>
            {
                conn2Proxy[m] = _args;
                return Task.CompletedTask;
            }));

            var hubId = await RelayService1.CreateHubAsync(null, default);

            await RelayService1.JoinHubAsync("conn1", hubId, null, default, default);

            Assert.Empty(conn2Proxy);
            AssertParticipantChanged(conn1Proxy, "conn1", ParticipantChangeType.Added);

            conn1Proxy.Clear();
            conn2Proxy.Clear();

            var relayHubInfo = await RelayService2.JoinHubAsync("conn2", hubId, null, new JoinOptions() { CreateIfNotExists = true }, default);
            Assert.Equal(2, relayHubInfo.Count);
            Assert.True(relayHubInfo.ContainsKey("conn1"));
            Assert.True(relayHubInfo.ContainsKey("conn2"));
            AssertParticipantChanged(conn1Proxy, "conn2", ParticipantChangeType.Added);
            AssertParticipantChanged(conn2Proxy, "conn2", ParticipantChangeType.Added);


            conn1Proxy.Clear();
            conn2Proxy.Clear();
            await RelayService1.SendDataHubAsync("conn1", hubId, SendOption.None, null, "type1", Encoding.UTF8.GetBytes("hi"), null, default);

            AssertDataReceived(conn1Proxy, hubId, "conn1");
            AssertDataReceived(conn2Proxy, hubId, "conn1");

            conn1Proxy.Clear();
            conn2Proxy.Clear();

            await RelayService1.UpdateAsync("conn1", hubId, new Dictionary<string, object>() 
            {
                { "prop1", 100 },
            }, default);
            AssertParticipantChanged(conn1Proxy, "conn1", ParticipantChangeType.Updated);
            AssertParticipantChanged(conn2Proxy, "conn1", ParticipantChangeType.Updated);

            conn1Proxy.Clear();
            conn2Proxy.Clear();
            await RelayService2.LeaveHubAsync("conn2", hubId, default);
            AssertParticipantChanged(conn1Proxy, "conn2", ParticipantChangeType.Removed);
            AssertParticipantChanged(conn2Proxy, "conn2", ParticipantChangeType.Removed);

            // re-join
            await RelayService2.JoinHubAsync("conn2", hubId, null, default, default);

            conn1Proxy.Clear();
            conn2Proxy.Clear();
            await RelayService1.DeleteHubAsync(hubId, default);
            AssertDeleted(conn1Proxy, hubId);
            AssertDeleted(conn2Proxy, hubId);
        }

        [Fact]
        public async Task TestCreate()
        {
            ClientProxies1.Add("conn1", MockUtils.CreateClientProxy((m, _args) =>
            {
                return Task.CompletedTask;
            }));
            ClientProxies2.Add("conn2", MockUtils.CreateClientProxy((m, _args) =>
            {
                return Task.CompletedTask;
            }));
            var hubId = await RelayService1.CreateHubAsync(null, default);
            var hubInfo = await RelayService2.JoinHubAsync("conn2", hubId, null, default, default);
            Assert.Single(hubInfo);
            hubInfo = await RelayService1.JoinHubAsync("conn1", hubId, null, default, default);
            Assert.Equal(2, hubInfo.Count);
        }

        [Fact]
        public async Task TestParticipants()
        {
            var conn1Proxy = new Dictionary<string, object[]>();
            ClientProxies1.Add("conn1", MockUtils.CreateClientProxy((m, _args) =>
            {
                conn1Proxy[m] = _args;
                return Task.CompletedTask;
            }));
            ClientProxies1.Add("conn3", MockUtils.CreateClientProxy((m, _args) =>
            {
                return Task.CompletedTask;
            }));
            var conn2Proxy = new Dictionary<string, object[]>();
            ClientProxies2.Add("conn2", MockUtils.CreateClientProxy((m, _args) =>
            {
                conn2Proxy[m] = _args;
                return Task.CompletedTask;
            }));


            var hubId = await RelayService1.CreateHubAsync(null, default);
            var participant2Properties = new Dictionary<string, object>()
            {
                { "prop1", 100 },
                { "prop2", "str2" },
            };

            var hubInfo = await RelayService2.JoinHubAsync("conn2", hubId, participant2Properties, default, default);
            Assert.Single(hubInfo);
            var participant1Properties = new Dictionary<string, object>()
            {
                { "prop1", 200 },
                { "prop2", "str1" },
            };
            hubInfo = await RelayService1.JoinHubAsync("conn1", hubId, participant1Properties, default, default);
            Assert.Equal(2, hubInfo.Count);

            var properties = AssertParticipantChanged(conn2Proxy, "conn1", ParticipantChangeType.Added);
            Assert.NotNull(properties);
            Assert.Equal(200, Convert.ToInt32(properties["prop1"]));
            Assert.Equal("str1", properties["prop2"]);

            participant1Properties = new Dictionary<string, object>()
            {
                { "prop3", true },
            };
            // rejoin same hub id with other properties
            await RelayService1.JoinHubAsync("conn1", hubId, participant1Properties, default, default);
            properties = AssertParticipantChanged(conn2Proxy, "conn1", ParticipantChangeType.Updated);
            Assert.True(properties.ContainsKey("prop1"));
            Assert.True(properties.ContainsKey("prop2"));
            Assert.Equal(true, properties["prop3"]);

            properties = AssertParticipantChanged(conn1Proxy, "conn1", ParticipantChangeType.Updated);
            Assert.Equal(3, properties.Count);

            // a new connection
            await RelayService1.JoinHubAsync("conn3", hubId, null, default, default);
            properties = AssertParticipantChanged(conn1Proxy, "conn3", ParticipantChangeType.Added);
            Assert.Null(properties);
            properties = AssertParticipantChanged(conn2Proxy, "conn3", ParticipantChangeType.Added);
            Assert.Null(properties);

            var participant3Properties = new Dictionary<string, object>()
            {
                { "prop1", 300 },
            };
            await RelayService1.UpdateAsync("conn3", hubId, participant3Properties, default);
            properties = AssertParticipantChanged(conn1Proxy, "conn3", ParticipantChangeType.Updated);
            Assert.Equal(300, Convert.ToInt32(properties["prop1"]));
            properties = AssertParticipantChanged(conn2Proxy, "conn3", ParticipantChangeType.Updated);
            Assert.Equal(300, Convert.ToInt32(properties["prop1"]));

            await RelayService1.LeaveHubAsync("conn3", hubId, default);
            AssertParticipantChanged(conn1Proxy, "conn3", ParticipantChangeType.Removed);
            AssertParticipantChanged(conn2Proxy, "conn3", ParticipantChangeType.Removed);
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
            Assert.Equal(1, args[2]);
            Assert.Equal("type1", args[3]);
            Assert.Equal("hi", Encoding.UTF8.GetString((byte[])args[4]));
        }

        private static Dictionary<string, object> AssertParticipantChanged(
            Dictionary<string, object[]> connProxy,
            string participantId,
            ParticipantChangeType changeType)
        {
            Assert.NotEmpty(connProxy);
            var args = connProxy[RelayHubMethods.MethodParticipantChanged];
            Assert.Equal(participantId, args[1]);
            Assert.Equal(changeType, args[3]);
            return args[2] as Dictionary<string, object>;
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
