using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.PresenceServiceHubTests
{
    public class ScaleServiceTests
    {
        private readonly Dictionary<string, IClientProxy> clientProxies1;
        private readonly Dictionary<string, IClientProxy> clientProxies2;
        private readonly PresenceService presenceService1;
        private readonly PresenceService presenceService2;

        public ScaleServiceTests()
        {
            this.clientProxies1 = new Dictionary<string, IClientProxy>();
            this.clientProxies2 = new Dictionary<string, IClientProxy>();

            var serviceLogger = new Mock<ILogger<PresenceService>>();

            this.presenceService1 = new PresenceService(MockUtils.CreateHubContextMock(this.clientProxies1), serviceLogger.Object);
            this.presenceService2 = new PresenceService(MockUtils.CreateHubContextMock(this.clientProxies2), serviceLogger.Object);
            var mockBackplaneProvider = new MockBackplaneProvider();

            this.presenceService1.AddBackplaneProvider(mockBackplaneProvider);
            this.presenceService2.AddBackplaneProvider(mockBackplaneProvider);
        }

        [Fact]
        public async Task Test()
        {
            (string, object[]) conn1Proxy = default;
            this.clientProxies1.Add("conn1", MockUtils.CreateClientProxy((m, args) =>
            {
                conn1Proxy = (m, args);
                return Task.CompletedTask;
            }));
            (string, object[]) conn2Proxy = default;
            this.clientProxies2.Add("conn1", MockUtils.CreateClientProxy((m, args) =>
            {
                conn2Proxy = (m, args);
                return Task.CompletedTask;
            }));

            await this.presenceService1.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);

            var contactProperties = await this.presenceService1.AddSubcriptionsAsync(AsContactRef("conn1", "contact1"), new ContactReference[] { AsContactRef(null, "contact2") }, new string[] { "status" });
            Assert.Null(contactProperties["contact2"]["status"]);

            conn1Proxy = default;
            await this.presenceService2.RegisterSelfContactAsync(AsContactRef("conn1", "contact2"), new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);

            AssertContactRef("conn1", "contact2", conn1Proxy.Item2[0]);
            var notifyProperties = conn1Proxy.Item2[1] as Dictionary<string, object>;
            Assert.Equal("available", notifyProperties["status"]);

            contactProperties = await this.presenceService2.AddSubcriptionsAsync(AsContactRef("conn1", "contact2"), new ContactReference[] { AsContactRef(null, "contact1") }, new string[] { "status" });
            Assert.Equal("available", contactProperties["contact1"]["status"]);

            conn2Proxy = default;
            await this.presenceService1.UpdatePropertiesAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);
            AssertContactRef("conn1", "contact1", conn2Proxy.Item2[0]);
            notifyProperties = conn2Proxy.Item2[1] as Dictionary<string, object>;
            Assert.Equal("busy", notifyProperties["status"]);

            conn2Proxy = default;
            await this.presenceService1.SendMessageAsync(AsContactRef("conn1", "contact1"), AsContactRef(null, "contact2"), "type1", JToken.FromObject(100), default);
            AssertContactRef("conn1", "contact2", conn2Proxy.Item2[0]);
            AssertContactRef("conn1", "contact1", conn2Proxy.Item2[1]);
            Assert.Equal("type1", conn2Proxy.Item2[2]);
        }

        [Fact]
        public async Task TestRequestSubscription()
        {
            (string, object[]) conn1Proxy = default;
            this.clientProxies1.Add("conn1", MockUtils.CreateClientProxy((m, args) =>
            {
                conn1Proxy = (m, args);
                return Task.CompletedTask;
            }));

            await this.presenceService1.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
               { "email", "contact1@microsoft.com" },
               { "status", "available" },
            }, CancellationToken.None);

            var results = await this.presenceService1.RequestSubcriptionsAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact2@microsoft.com" },
                }},
                new string[] { "status" }, useStubContact: true, CancellationToken.None);

            conn1Proxy = default;
            await this.presenceService2.RegisterSelfContactAsync(AsContactRef("conn1", "contact2"), new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            AssertContactRef("conn1", results[0][Properties.IdReserved].ToString(), conn1Proxy.Item2[0]);
            var notifyProperties = conn1Proxy.Item2[1] as Dictionary<string, object>;
            Assert.Equal("available", notifyProperties["status"]);

            conn1Proxy = default;
            await this.presenceService2.UpdatePropertiesAsync(AsContactRef("conn1", "contact2"), new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);
            AssertContactRef("conn1", results[0][Properties.IdReserved].ToString(), conn1Proxy.Item2[0]);
            notifyProperties = conn1Proxy.Item2[1] as Dictionary<string, object>;
            Assert.Equal("busy", notifyProperties["status"]);

            // this should remove the stub contact 
            this.presenceService1.RemoveSubscription(AsContactRef("conn1", "contact1"), new ContactReference[] { AsContactRef(null, results[0][Properties.IdReserved].ToString()) });
            results = await this.presenceService1.RequestSubcriptionsAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact2@microsoft.com" },
                }},
                new string[] { "status" }, useStubContact: true, CancellationToken.None);

            Assert.Single(results);
            Assert.Equal("contact2", results[0][Properties.IdReserved]);
            Assert.Equal("busy", results[0]["status"]);

            conn1Proxy = default;
            await this.presenceService2.UpdatePropertiesAsync(AsContactRef("conn1", "contact2"), new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);
            AssertContactRef("conn1", "contact2", conn1Proxy.Item2[0]);
            notifyProperties = conn1Proxy.Item2[1] as Dictionary<string, object>;
            Assert.Equal("available", notifyProperties["status"]);
        }

        [Fact]
        public async Task AddConnectionSubscription()
        {
            (string, object[]) connProxy = default;
            this.clientProxies1.Add("conn1", MockUtils.CreateClientProxy((m, args) =>
            {
                connProxy = (m, args);
                return Task.CompletedTask;
            }));

            await this.presenceService1.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService2.RegisterSelfContactAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService2.RegisterSelfContactAsync(AsContactRef("conn3", "contact2"), new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);

            var contactProperties = await this.presenceService1.AddSubcriptionsAsync(
                AsContactRef("conn1", "contact1"),
                new ContactReference[] { AsContactRef("conn2", "contact2") }, new string[] { "status" });
            Assert.NotNull(contactProperties);
            Assert.Single(contactProperties);
            Assert.Equal("available", contactProperties["contact2"]["status"]);

            connProxy = default;
            await this.presenceService2.UpdatePropertiesAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "status", "dnd" },
            }, CancellationToken.None);
            Assert.NotNull(connProxy.Item1);
            Assert.Equal(connProxy.Item1, Methods.UpdateValues);
            var notifyProperties = connProxy.Item2[1] as Dictionary<string, object>;
            Assert.Equal("dnd", notifyProperties["status"]);

            connProxy = default;
            await this.presenceService2.UpdatePropertiesAsync(AsContactRef("conn3", "contact2"), new Dictionary<string, object>()
            {
                { "status", "away" },
            }, CancellationToken.None);
            Assert.Null(connProxy.Item1);
        }

        [Fact]
        public async Task SendMessageToConnection()
        {
            (string, object[]) conn2Proxy = default;
            this.clientProxies2.Add("conn2", MockUtils.CreateClientProxy((m, args) =>
            {
                conn2Proxy = (m, args);
                return Task.CompletedTask;
            }));

            (string, object[]) conn3Proxy = default;
            this.clientProxies2.Add("conn3", MockUtils.CreateClientProxy((m, args) =>
            {
                conn3Proxy = (m, args);
                return Task.CompletedTask;
            }));

            await this.presenceService1.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), null, CancellationToken.None);
            await this.presenceService2.RegisterSelfContactAsync(AsContactRef("conn2", "contact2"), null, CancellationToken.None);
            await this.presenceService2.RegisterSelfContactAsync(AsContactRef("conn3", "contact2"), null, CancellationToken.None);

            conn2Proxy = default;
            conn3Proxy = default;

            await this.presenceService1.SendMessageAsync(
                AsContactRef("conn1", "contact1"),
                AsContactRef("conn2", "contact2"),
                "raw",
                100,
                CancellationToken.None);
            Assert.NotNull(conn2Proxy.Item1);
            Assert.Null(conn3Proxy.Item1);

            conn2Proxy = default;
            conn3Proxy = default;

            await this.presenceService1.SendMessageAsync(
                AsContactRef("conn1", "contact1"),
                AsContactRef("conn3", "contact2"),
                "raw",
                100,
                CancellationToken.None);
            Assert.Null(conn2Proxy.Item1);
            Assert.NotNull(conn3Proxy.Item1);
        }

        private static ContactReference AsContactRef(string connectionId, string id) => new ContactReference(id, connectionId);
        private static void AssertContactRef(string connectionId, string id, ContactReference contactReference)
        {
            Assert.Equal(AsContactRef(connectionId, id), contactReference);
        }

        private static void AssertContactRef(string connectionId, string id, object contactReference)
        {
            Assert.IsType<ContactReference>(contactReference);
            AssertContactRef(connectionId, id, (ContactReference)contactReference);
        }

        private class MockBackplaneProvider : IBackplaneProvider
        {
            private readonly Dictionary<string, ContactData> contactDataMap;
            private readonly List<OnContactChangedAsync> contactChangedAsyncs = new List<OnContactChangedAsync>();
            private readonly List<OnMessageReceivedAsync> messageReceivedAsyncs = new List<OnMessageReceivedAsync>();

            internal MockBackplaneProvider()
            {
                this.contactDataMap = new Dictionary<string, ContactData>();
            }

            public OnContactChangedAsync ContactChangedAsync
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    this.contactChangedAsyncs.Add(value);
                }
            }

            public OnMessageReceivedAsync MessageReceivedAsync
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    this.messageReceivedAsyncs.Add(value);
                }
            }

            public int Priority => 0;

            public Task<ContactData> GetContactPropertiesAsync(string contactId, CancellationToken cancellationToken)
            {
                if (this.contactDataMap.TryGetValue(contactId, out var properties))
                {
                    return Task.FromResult(properties);
                }

                return Task.FromResult<ContactData>(null);
            }

            public async Task SendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
            {
                await Task.WhenAll(this.messageReceivedAsyncs.Select(c => c.Invoke(sourceId, messageData, cancellationToken)));
            }

            public async Task UpdateContactAsync(string sourceId, string connectionId, ContactData contactData, ContactUpdateType updateContactType, CancellationToken cancellationToken)
            {
                this.contactDataMap[contactData.Id] = contactData;

                await Task.WhenAll(this.contactChangedAsyncs.Select(c => c.Invoke(sourceId, connectionId, contactData, updateContactType, cancellationToken)));
            }

            public Task<ContactData[]> GetContactsAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken)
            {
                var matchContacts = this.contactDataMap
                    .Where(kvp => matchProperties.MatchProperties(kvp.Value.Properties))
                    .Select(kvp =>
                    {
                        kvp.Value.Properties[Properties.IdReserved] = kvp.Key;
                        return kvp.Value;
                    }).ToArray();

                return Task.FromResult(matchContacts);
            }
        }
    }
}
