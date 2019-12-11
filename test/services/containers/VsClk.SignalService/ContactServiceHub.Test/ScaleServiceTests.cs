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
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    public class ScaleServiceTests : TestBase
    {
        private readonly Dictionary<string, IClientProxy> clientProxies1;
        private readonly Dictionary<string, IClientProxy> clientProxies2;
        private readonly ContactService presenceService1;
        private readonly ContactService presenceService2;

        public ScaleServiceTests()
        {
            this.clientProxies1 = new Dictionary<string, IClientProxy>();
            this.clientProxies2 = new Dictionary<string, IClientProxy>();

            var serviceLogger = new Mock<ILogger<ContactService>>();
            var backplaneServiceManagerLogger = new Mock<ILogger<ContactBackplaneManager>>();

            this.presenceService1 = new ContactService(
                new HubServiceOptions() { Id = "mock1" },
                MockUtils.CreateSingleHubContextHostMock<ContactServiceHub>(this.clientProxies1),
                serviceLogger.Object,
                new ContactBackplaneManager(backplaneServiceManagerLogger.Object));

            this.presenceService2 = new ContactService(
                new HubServiceOptions() { Id = "mock2" },
                MockUtils.CreateSingleHubContextHostMock<ContactServiceHub>(this.clientProxies2),
                serviceLogger.Object,
                new ContactBackplaneManager(backplaneServiceManagerLogger.Object));
            var mockBackplaneProvider = new MockBackplaneProvider();

            this.presenceService1.BackplaneManager.RegisterProvider(mockBackplaneProvider);
            this.presenceService2.BackplaneManager.RegisterProvider(mockBackplaneProvider);
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
            var conn1Proxy = new Dictionary<string, object[]>();
            this.clientProxies1.Add("conn1-1", MockUtils.CreateClientProxy((m, args) =>
            {
                conn1Proxy[m] = args;
                return Task.CompletedTask;
            }));

            var contact1_1_Ref = AsContactRef("conn1-1", "contact1");
            var contact2_2_Ref = AsContactRef("conn1-2", "contact2");

            await this.presenceService1.RegisterSelfContactAsync(contact1_1_Ref, new Dictionary<string, object>()
            {
               { "email", "contact1@microsoft.com" },
               { "status", "available" },
            }, CancellationToken.None);

            var results = await this.presenceService1.RequestSubcriptionsAsync(contact1_1_Ref, new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact2@microsoft.com" },
                }},
                new string[] { "status" }, useStubContact: true, CancellationToken.None);

            conn1Proxy.Clear();
            await this.presenceService2.RegisterSelfContactAsync(contact2_2_Ref, new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            Assert.True(conn1Proxy.ContainsKey(ContactHubMethods.UpdateValues));
            var items = conn1Proxy[ContactHubMethods.UpdateValues];

            AssertContactRef("conn1-2", results[0][ContactProperties.IdReserved].ToString(), items[0]);
            var notifyProperties = items[1] as Dictionary<string, object>;
            Assert.Equal("available", notifyProperties["status"]);


            conn1Proxy.Clear();
            await this.presenceService2.UnregisterSelfContactAsync(contact2_2_Ref, null, default);
            Assert.True(conn1Proxy.ContainsKey(ContactHubMethods.UpdateValues));
            items = conn1Proxy[ContactHubMethods.UpdateValues];
            notifyProperties = items[1] as Dictionary<string, object>;
            Assert.Null(notifyProperties["status"]);

            await this.presenceService2.RegisterSelfContactAsync(contact2_2_Ref, new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            conn1Proxy.Clear();
            await this.presenceService2.UpdatePropertiesAsync(contact2_2_Ref, new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);

            Assert.True(conn1Proxy.ContainsKey(ContactHubMethods.UpdateValues));
            items = conn1Proxy[ContactHubMethods.UpdateValues];
            AssertContactRef("conn1-2", results[0][ContactProperties.IdReserved].ToString(), items[0]);
            notifyProperties = items[1] as Dictionary<string, object>;
            Assert.Equal("busy", notifyProperties["status"]);

            // this should remove the stub contact 
            this.presenceService1.RemoveSubscription(contact1_1_Ref, new ContactReference[] { AsContactRef(null, results[0][ContactProperties.IdReserved].ToString()) });
            results = await this.presenceService1.RequestSubcriptionsAsync(contact1_1_Ref, new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact2@microsoft.com" },
                }},
                new string[] { "status" }, useStubContact: true, CancellationToken.None);

            Assert.Single(results);
            Assert.Equal("contact2", results[0][ContactProperties.IdReserved]);
            Assert.Equal("busy", results[0]["status"]);

            conn1Proxy.Clear();
            await this.presenceService2.UpdatePropertiesAsync(contact2_2_Ref, new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);

            Assert.True(conn1Proxy.ContainsKey(ContactHubMethods.UpdateValues));
            items = conn1Proxy[ContactHubMethods.UpdateValues];

            Assert.Equal(contact2_2_Ref, items[0]);
            notifyProperties = items[1] as Dictionary<string, object>;
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
            Assert.Equal(connProxy.Item1, ContactHubMethods.UpdateValues);
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

        [Fact]
        public async Task SelfTest()
        {
            var conn1Callback = new Dictionary<string, object[]>();
            this.clientProxies1.Add("conn1-1", MockUtils.CreateClientProxy((m, args) =>
            {
                conn1Callback[m] = args;
                return Task.CompletedTask;
            }));
            var conn2Callback = new Dictionary<string, object[]>();
            this.clientProxies2.Add("conn1-2", MockUtils.CreateClientProxy((m, args) =>
            {
                conn2Callback[m] = args;
                return Task.CompletedTask;
            }));

            var contact1_1_Ref = AsContactRef("conn1-1", "contact1");
            var contact1_2_Ref = AsContactRef("conn1-2", "contact1");

            await this.presenceService1.RegisterSelfContactAsync(contact1_1_Ref, new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService2.RegisterSelfContactAsync(contact1_2_Ref, new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);

            Assert.True(conn1Callback.ContainsKey(ContactHubMethods.UpdateValues));
            var notifyProperties = conn1Callback[ContactHubMethods.UpdateValues][1] as Dictionary<string, object>;
            Assert.Equal("busy", notifyProperties["status"]);

            conn2Callback.Clear();
            await this.presenceService1.UpdatePropertiesAsync(contact1_1_Ref, new Dictionary<string, object>()
            {
                { "other", 100 },
            }, CancellationToken.None);

            Assert.True(conn2Callback.ContainsKey(ContactHubMethods.UpdateValues));
            Assert.Equal(contact1_1_Ref, conn2Callback[ContactHubMethods.UpdateValues][0]);
            notifyProperties = (Dictionary<string, object>)conn2Callback[ContactHubMethods.UpdateValues][1];
            Assert.True(notifyProperties.ContainsKey("other"));
            Assert.Equal(100, notifyProperties["other"]);

            conn2Callback.Clear();
            await this.presenceService1.SendMessageAsync(contact1_1_Ref, contact1_2_Ref, "type1", "hi", CancellationToken.None);
            Assert.True(conn2Callback.ContainsKey(ContactHubMethods.ReceiveMessage));

            Assert.Equal(contact1_2_Ref, conn2Callback[ContactHubMethods.ReceiveMessage][0]);
            Assert.Equal(contact1_1_Ref, conn2Callback[ContactHubMethods.ReceiveMessage][1]);
            Assert.Equal("type1", conn2Callback[ContactHubMethods.ReceiveMessage][2]);
            Assert.Equal("hi", conn2Callback[ContactHubMethods.ReceiveMessage][3]);
        }

        [Fact]
        public async Task UnregisterTest()
        {
            var conn1Callback = new Dictionary<string, object[]>();
            this.clientProxies1.Add("conn1-1", MockUtils.CreateClientProxy((m, args) =>
            {
                conn1Callback[m] = args;
                return Task.CompletedTask;
            }));
            var conn2Callback = new Dictionary<string, object[]>();
            this.clientProxies2.Add("conn1-2", MockUtils.CreateClientProxy((m, args) =>
            {
                conn2Callback[m] = args;
                return Task.CompletedTask;
            }));

            var contact1_1_Ref = AsContactRef("conn1-1", "contact1");
            var contact2_2_Ref = AsContactRef("conn1-2", "contact2");

            await this.presenceService1.RegisterSelfContactAsync(contact1_1_Ref, new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService2.RegisterSelfContactAsync(contact2_2_Ref, new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);

            var contactProperties = await this.presenceService1.AddSubcriptionsAsync(contact1_1_Ref, new ContactReference[] { AsContactRef(null, "contact2") }, new string[] { "status" });
            Assert.Equal("busy", contactProperties["contact2"]["status"]);

            conn1Callback.Clear();
            conn2Callback.Clear();
            await this.presenceService2.UnregisterSelfContactAsync(contact2_2_Ref, null, CancellationToken.None);

            Assert.True(conn1Callback.ContainsKey(ContactHubMethods.UpdateValues));
            var notifyProperties = conn1Callback[ContactHubMethods.UpdateValues][1] as Dictionary<string, object>;
            Assert.Null(notifyProperties["status"]);
        }

        [Fact]
        public async Task SelfConnections()
        {
            var contact1_1_Ref = AsContactRef("conn1-1", "contact1");
            var contact1_2_Ref = AsContactRef("conn1-2", "contact1");

            await this.presenceService1.RegisterSelfContactAsync(contact1_1_Ref, new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService2.RegisterSelfContactAsync(contact1_2_Ref, new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);

            var selfConnections = await this.presenceService1.GetSelfConnectionsAsync("contact1", CancellationToken.None);
            Assert.True(selfConnections.ContainsKey("conn1-1"));
            Assert.True(selfConnections.ContainsKey("conn1-2"));

            selfConnections = await this.presenceService2.GetSelfConnectionsAsync("contact1", CancellationToken.None);
            Assert.True(selfConnections.ContainsKey("conn1-1"));
            Assert.True(selfConnections.ContainsKey("conn1-2"));
        }

        [Fact]
        public async Task AllProperties()
        {
            var conn1Proxy = new Dictionary<string, object[]>();
            this.clientProxies1.Add("conn1", MockUtils.CreateClientProxy((m, args) =>
            {
                conn1Proxy[m] = args;
                return Task.CompletedTask;
            }));

            var contact1Ref = AsContactRef("conn1", "contact1");
            var contact2Ref = AsContactRef("conn2", "contact2");
            await this.presenceService1.RegisterSelfContactAsync(contact1Ref, null, CancellationToken.None);

            await this.presenceService2.RegisterSelfContactAsync(contact2Ref, new Dictionary<string, object>()
            {
                { "property0", 10 },
            }, CancellationToken.None);

            var result = await this.presenceService1.AddSubcriptionsAsync(
                contact1Ref,
                new ContactReference[] { contact2Ref }, new string[] { "*" });

            var resultProperties = result[contact2Ref.Id];
            Assert.Equal(10, resultProperties["property0"]);

            conn1Proxy.Clear();
            await this.presenceService2.UpdatePropertiesAsync(contact2Ref, new Dictionary<string, object>()
            {
                { "property1", 100 },
                { "property2", "hello" },
            }, CancellationToken.None);

            Assert.True(conn1Proxy.ContainsKey(ContactHubMethods.UpdateValues));
            var notifyProperties = conn1Proxy[ContactHubMethods.UpdateValues][1] as Dictionary<string, object>;
            Assert.Equal(2, notifyProperties.Count);
            Assert.Equal(100, notifyProperties["property1"]);
            Assert.Equal("hello", notifyProperties["property2"]);

            conn1Proxy.Clear();
            await this.presenceService2.UpdatePropertiesAsync(contact2Ref, new Dictionary<string, object>()
            {
                { "property3", true },
            }, CancellationToken.None);
            notifyProperties = conn1Proxy[ContactHubMethods.UpdateValues][1] as Dictionary<string, object>;
            Assert.Single(notifyProperties);
            Assert.Equal(true, notifyProperties["property3"]);
        }

        [Fact]
        public async Task SelfConnectionsWithException()
        {
            bool throwIf = false;
            this.clientProxies1.Add("conn2", MockUtils.CreateClientProxy((m, args) =>
            {
                if (throwIf)
                {
                    throw new Exception("failed");
                }

                return Task.CompletedTask;
            }));

            var contact1Ref = AsContactRef("conn1", "contact1");
            var contact2Ref = AsContactRef("conn2", "contact2");
            await this.presenceService1.RegisterSelfContactAsync(contact1Ref, new Dictionary<string, object>()
            {
                { "property1", 10 },
            }, CancellationToken.None);
            await this.presenceService1.RegisterSelfContactAsync(contact2Ref, null, CancellationToken.None);
            await this.presenceService1.AddSubcriptionsAsync(contact2Ref, new ContactReference[] { contact1Ref }, new string[] { "*" }, CancellationToken.None);

            throwIf = true;
            await Assert.ThrowsAnyAsync<Exception>(() => this.presenceService1.UnregisterSelfContactAsync(contact1Ref, null, CancellationToken.None));
            throwIf = false;

            var contact1_2Ref = AsContactRef("conn3", "contact1");
            await this.presenceService2.RegisterSelfContactAsync(contact1_2Ref, null, CancellationToken.None);
            await this.presenceService2.UnregisterSelfContactAsync(contact1_2Ref, null, CancellationToken.None);

            var selfConnections = await this.presenceService1.GetSelfConnectionsAsync("contact1", CancellationToken.None);
            Assert.Empty(selfConnections);
        }

        private class MockBackplaneProvider : IContactBackplaneProvider
        {
            private readonly Dictionary<string, ContactDataInfo> contactDataMap;
            private readonly List<OnContactChangedAsync> contactChangedAsyncs = new List<OnContactChangedAsync>();
            private readonly List<OnMessageReceivedAsync> messageReceivedAsyncs = new List<OnMessageReceivedAsync>();

            internal MockBackplaneProvider()
            {
                this.contactDataMap = new Dictionary<string, ContactDataInfo>();
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

            public Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken)
            {
                if (this.contactDataMap.TryGetValue(contactId, out var contactDataInfo))
                {
                    return Task.FromResult(contactDataInfo);
                }

                return Task.FromResult<ContactDataInfo>(null);
            }

            public async Task SendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
            {
                await Task.WhenAll(this.messageReceivedAsyncs.Select(c => c.Invoke(sourceId, messageData, cancellationToken)));
            }

            public async Task<ContactDataInfo> UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
            {
                ContactDataInfo contactDataInfo;
                if (!this.contactDataMap.TryGetValue(contactDataChanged.ContactId, out contactDataInfo))
                {
                    contactDataInfo = new Dictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>();
                    this.contactDataMap[contactDataChanged.ContactId] = contactDataInfo;
                }

                contactDataInfo.UpdateConnectionProperties(contactDataChanged);
                var contactDataInfoChanged = new ContactDataChanged<ContactDataInfo>(
                    CreateChangeId(),
                    contactDataChanged.ServiceId,
                    contactDataChanged.ConnectionId,
                    contactDataChanged.ContactId,
                    contactDataChanged.ChangeType,
                    contactDataInfo);

                await Task.WhenAll(this.contactChangedAsyncs.Select(c => c.Invoke(contactDataInfoChanged, contactDataChanged.Data.Keys.ToArray(), cancellationToken)));
                return contactDataInfo;
            }

            public Task<Dictionary<string, ContactDataInfo>[]> GetContactsDataAsync(Dictionary<string, object>[] matchProperties, CancellationToken cancellationToken)
            {
                var matchContacts = matchProperties.Select(item => this.contactDataMap
                    .Where(kvp => item.MatchProperties(kvp.Value.GetAggregatedProperties()))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)).ToArray();

                return Task.FromResult(matchContacts);
            }

            public Task DisposeDataChangesAsync(DataChanged[] dataChanges, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task UpdateMetricsAsync((string ServiceId, string Stamp) serviceInfo, ContactServiceMetrics metrics, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public bool HandleException(string methodName, Exception error)
            {
                throw new NotImplementedException();
            }
        }
    }
}
