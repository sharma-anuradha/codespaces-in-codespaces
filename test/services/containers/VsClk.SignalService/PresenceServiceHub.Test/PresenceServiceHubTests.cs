using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.PresenceServiceHubTests
{
    public class PresenceServiceHubTests
    {
        private readonly Dictionary<string, IClientProxy> clientProxies;
        private readonly PresenceService presenceService;
        private readonly ILogger<PresenceService> presenceServiceLogger;

        public PresenceServiceHubTests()
        {
            this.clientProxies = new Dictionary<string, IClientProxy>();
            this.presenceServiceLogger = new Mock<ILogger<PresenceService>>().Object;

            var trace = new TraceSource("PresenceServiceHubTests");
            this.presenceService = new PresenceService(
                new PresenceServiceOptions() { Id = "mock" },
                MockUtils.CreateSingleHubContextHostMock<PresenceServiceHub>(this.clientProxies),
                this.presenceServiceLogger);
        }

        [Fact]
        public async Task RegisterTest()
        {
            var conn1Proxy = new Dictionary<string, object[]>();
            this.clientProxies.Add("conn1", MockUtils.CreateClientProxy((m, args) =>
            {
                conn1Proxy[m] = args;
                return Task.CompletedTask;
            }));
            var conn2Proxy = new Dictionary<string, object[]>();
            this.clientProxies.Add("conn2", MockUtils.CreateClientProxy((m, args) =>
            {
                conn2Proxy[m] = args;
                return Task.CompletedTask;
            }));

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
                { "other", 100 },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            var contactProperties = await this.presenceService.AddSubcriptionsAsync(
                AsContactRef("conn2", "contact2"),
                new ContactReference[] { AsContactRef(null, "contact1") }, new string[] { "status", "other" });
            Assert.NotNull(contactProperties);
            Assert.Single(contactProperties);

            var contact1PropertiesExpected = new Dictionary<string, object>()
            {
                { "status", "available" },
                { "other", 100 },
            };

            contactProperties["contact1"].All(e => contact1PropertiesExpected.Contains(e));

            await this.presenceService.UpdatePropertiesAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);

            Assert.True(conn1Proxy.ContainsKey(PresenceHubMethods.UpdateValues));
            Assert.Equal(3, conn2Proxy[PresenceHubMethods.UpdateValues].Length);
            AssertContactRef("conn1", "contact1", conn2Proxy[PresenceHubMethods.UpdateValues][0]);
            var notifyProperties = conn2Proxy[PresenceHubMethods.UpdateValues][1] as Dictionary<string, object>;

            await this.presenceService.AddSubcriptionsAsync(
                AsContactRef("conn2", "contact2"),
                new ContactReference[] { AsContactRef(null, "contact1") }, new string[] { "other" });

            conn2Proxy.Clear();
            await this.presenceService.UpdatePropertiesAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);

            Assert.Empty(conn2Proxy);
            await this.presenceService.UpdatePropertiesAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "other", 200 },
            }, CancellationToken.None);
            Assert.True(conn2Proxy.ContainsKey(PresenceHubMethods.UpdateValues));

            this.presenceService.RemoveSubscription(AsContactRef("conn2", "contact2"), new ContactReference[] { AsContactRef(null, "contact1") });
            conn2Proxy.Clear();
            await this.presenceService.UpdatePropertiesAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "status", "busy" },
                { "other", 300 },
            }, CancellationToken.None);
            Assert.Empty(conn2Proxy);

            conn2Proxy.Clear();
            await this.presenceService.AddSubcriptionsAsync(AsContactRef("conn2", "contact2"), new ContactReference[] { AsContactRef(null, "contact1") }, new string[] { "status", "other" });

            // unregister with reconnection should not affect "conn2"
            await this.presenceService.UnregisterSelfContactAsync(AsContactRef("conn1", "contact1"), async (p) =>
            {
                // before removing we will add a new connection
                await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn3", "contact1"), new Dictionary<string, object>()
                {
                    { "email", "contact1@microsoft.com" },
                    { "status", "busy" },
                    { "other", 300 },
                }, CancellationToken.None);
                conn2Proxy.Clear();
            }, CancellationToken.None);

            await this.presenceService.UnregisterSelfContactAsync(AsContactRef("conn1", "contact1"), null, CancellationToken.None);
            Assert.True(conn2Proxy.ContainsKey(PresenceHubMethods.ConnectionChanged));

            // clear 'conn3'
            await this.presenceService.UnregisterSelfContactAsync(AsContactRef("conn3", "contact1"), null, CancellationToken.None);
            // publish 'contact1' & 'conn1'
            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
                { "other", 100 },
            }, CancellationToken.None);
            conn2Proxy.Clear();

            await this.presenceService.UnregisterSelfContactAsync(AsContactRef("conn1", "contact1"), null, CancellationToken.None);
            Assert.True(conn2Proxy.ContainsKey(PresenceHubMethods.UpdateValues));
            Assert.Equal(3, conn2Proxy[PresenceHubMethods.UpdateValues].Length);
            AssertContactRef("conn1", "contact1", conn2Proxy[PresenceHubMethods.UpdateValues][0]);
            notifyProperties = (Dictionary<string, object>)conn2Proxy[PresenceHubMethods.UpdateValues][1];
            Assert.Null(notifyProperties["other"]);
            Assert.Null(notifyProperties["status"]);
        }

        [Fact]
        public async Task TwoConnectionsTest()
        {
            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "value", 100 },
            }, CancellationToken.None);
            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn2", "contact1"), new Dictionary<string, object>()
            {
                { "value", 200 },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn3", "contact2"), null, CancellationToken.None);
            var subscriptionsResults = await this.presenceService.AddSubcriptionsAsync(AsContactRef("conn3", "contact2"), new ContactReference[] { AsContactRef(null, "contact1") }, new string[] { "value" });
            Assert.Equal(200, subscriptionsResults["contact1"]["value"]);

            this.presenceService.RemoveSubscription(AsContactRef("conn3", "contact2"), new ContactReference[] { AsContactRef(null, "contact1") });
            await this.presenceService.UnregisterSelfContactAsync(AsContactRef("conn2", "contact1"), null, CancellationToken.None);
            subscriptionsResults = await this.presenceService.AddSubcriptionsAsync(AsContactRef("conn3", "contact2"), new ContactReference[] { AsContactRef(null, "contact1") }, new string[] { "value" });
            Assert.Equal(100, subscriptionsResults["contact1"]["value"]);
        }

        [Fact]
        public async Task MatchMultipleContactsTest()
        {
            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "busy" },
            }, CancellationToken.None);

            var results = await this.presenceService.MatchContactsAsync(new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact1@microsoft.com" },
                },
                new Dictionary<string, object>()
                {
                    { "email", "contact2@microsoft.com" },
                } });
            Assert.Equal(2, results.Length);
            Assert.Single(results[0]);
            Assert.Single(results[1]);
            Assert.True(results[0].ContainsKey("contact1"));
            Assert.True(results[1].ContainsKey("contact2"));
        }

        [Fact]
        public async Task SearchContactsTest()
        {
            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "name", "Contact1" },
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "name", "Contact2" },
                { "email", "contact2@microsoft.com" },
                { "status", "busy" },
            }, CancellationToken.None);

            var results = await this.presenceService.SearchContactsAsync(new Dictionary<string, SearchProperty>
            {
                {
                    "email", new SearchProperty()
                    {
                        Expression = "^contact1"
                    }
                },
            }, null);
            Assert.Single(results);
            results = await this.presenceService.SearchContactsAsync(new Dictionary<string, SearchProperty>
            {
                {
                    "email", new SearchProperty()
                    {
                        Expression = "^CONTACT1",
                        Options = (int)(RegexOptions.IgnoreCase)
                    }
                },
            }, null);
            Assert.Single(results);
            results = await this.presenceService.SearchContactsAsync(new Dictionary<string, SearchProperty>
            {
                {
                    "email", new SearchProperty()
                    {
                        Expression = "@microsoft.com$"
                    }
                },
            }, null);
            Assert.Equal(2, results.Count);

            results = await this.presenceService.SearchContactsAsync(new Dictionary<string, SearchProperty>
            {
                {
                    "email", new SearchProperty()
                    {
                        Expression = "@microsoft.com$"
                    }
                },
                {
                    "status", new SearchProperty()
                    {
                        Expression = "^busy$"
                    }
                },
            }, null);
            Assert.Single(results);
        }

        [Fact]
        public async Task RequestSubcriptionsTest()
        {
            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "busy" },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn3", "contact3"), new Dictionary<string, object>()
            {
                { "email", "contact3@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn4", "contact4"), new Dictionary<string, object>()
            {
                { "email", "contact4@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            var results = await this.presenceService.RequestSubcriptionsAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact2@microsoft.com" },
                },
                new Dictionary<string, object>()
                {
                    { "email", "unknown@microsoft.com" },
                },
                new Dictionary<string, object>()
                {
                    { "email", "contact3@microsoft.com" },
                },
                new Dictionary<string, object>()
                {
                    { Properties.IdReserved, "contact4" },
                } },
                new string[] { "status" }, false, CancellationToken.None);
            Assert.Equal(4, results.Length);
            Assert.NotNull(results[0]);
            Assert.NotNull(results[2]);
            Assert.NotNull(results[3]);
            Assert.Null(results[1]);
            Assert.Equal("contact2", results[0][Properties.IdReserved]);
            Assert.Equal("busy", results[0]["status"]);
            Assert.Equal("contact3", results[2][Properties.IdReserved]);
            Assert.Equal("available", results[2]["status"]);
            Assert.Equal("contact4", results[3][Properties.IdReserved]);
            Assert.Equal("available", results[3]["status"]);
        }

        [Fact]
        public async Task CustomRequestSubcriptionsTest()
        {
            var customMatchService = new CustomMatchService(this.presenceService.HubContextHosts, this.presenceServiceLogger);

            await customMatchService.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            await customMatchService.RegisterSelfContactAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "busy" },
            }, CancellationToken.None);

            await customMatchService.RegisterSelfContactAsync(AsContactRef("conn3", "contact3"), new Dictionary<string, object>()
            {
                { "email", "contact3@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            await customMatchService.RegisterSelfContactAsync(AsContactRef("conn4", "contact4"), new Dictionary<string, object>()
            {
                { "email", "contact4@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            var results = await customMatchService.RequestSubcriptionsAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact2@microsoft.com" },
                },
                new Dictionary<string, object>()
                {
                    { "email", "contact4-alternate@microsoft.com" },
                } },
                new string[] { "status" }, false, CancellationToken.None);
            Assert.Null(results[0]);
            Assert.NotNull(results[1]);
            Assert.Equal("contact4", results[1][Properties.IdReserved]);
            Assert.Equal("available", results[1]["status"]);
        }

        [Fact]
        public async Task RequestSubcriptionsNoMatchingTest()
        {
            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            var results = await this.presenceService.RequestSubcriptionsAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact2@microsoft.com" },
                }},
                new string[] { "status" }, useStubContact: true, CancellationToken.None);
            Assert.Single(results);
            Assert.NotNull(results[0]);

            (string, object[]) conn1Proxy = default;
            this.clientProxies.Add("conn1", MockUtils.CreateClientProxy((m, args) =>
            {
                conn1Proxy = (m, args);
                return Task.CompletedTask;
            }));

            (string, object[]) conn2Proxy = default;
            this.clientProxies.Add("conn2", MockUtils.CreateClientProxy((m, args) =>
            {
                conn2Proxy = (m, args);
                return Task.CompletedTask;
            }));

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "busy" },
            }, CancellationToken.None);

            Assert.NotNull(conn1Proxy.Item1);
            Assert.Equal(conn1Proxy.Item1, PresenceHubMethods.UpdateValues);
            AssertContactRef("conn2", results[0][Properties.IdReserved].ToString(), conn1Proxy.Item2[0]);
            conn1Proxy = default;
            await this.presenceService.UpdatePropertiesAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);
            Assert.NotNull(conn1Proxy.Item1);
            Assert.Equal(conn1Proxy.Item1, PresenceHubMethods.UpdateValues);
            AssertContactRef("conn2", results[0][Properties.IdReserved].ToString(),conn1Proxy.Item2[0]);

            await this.presenceService.SendMessageAsync(AsContactRef("conn1", "contact1"), AsContactRef(null, results[0][Properties.IdReserved].ToString()), "raw", 100, CancellationToken.None);
            Assert.NotNull(conn2Proxy.Item1);
            Assert.Equal(conn2Proxy.Item1, PresenceHubMethods.ReceiveMessage);
            AssertContactRef("conn2", "contact2", conn2Proxy.Item2[0]);
            AssertContactRef("conn1", "contact1", conn2Proxy.Item2[1]);

            this.presenceService.RemoveSubscription(AsContactRef("conn1", "contact1"), new ContactReference[] { AsContactRef(null, results[0][Properties.IdReserved].ToString()) });
            results = await this.presenceService.RequestSubcriptionsAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact2@microsoft.com" },
                }},
                new string[] { "status" }, useStubContact: true, CancellationToken.None);
            Assert.Single(results);
            Assert.Equal("contact2", results[0][Properties.IdReserved]);
        }

        [Fact]
        public async Task AddConnectionSubscription()
        {
            (string, object[]) connProxy = default;
            this.clientProxies.Add("conn1", MockUtils.CreateClientProxy((m, args) =>
            {
                connProxy = (m, args);
                return Task.CompletedTask;
            }));

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn3", "contact2"), new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);

            var contactProperties = await this.presenceService.AddSubcriptionsAsync(
                AsContactRef("conn1", "contact1"),
                new ContactReference[] { AsContactRef("conn2", "contact2") }, new string[] { "status" });
            Assert.NotNull(contactProperties);
            Assert.Single(contactProperties);
            Assert.Equal("available", contactProperties["contact2"]["status"]);

            connProxy = default;
            await this.presenceService.UpdatePropertiesAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "status", "dnd" },
            }, CancellationToken.None);
            Assert.NotNull(connProxy.Item1);
            Assert.Equal(connProxy.Item1, PresenceHubMethods.UpdateValues);
            var notifyProperties = connProxy.Item2[1] as Dictionary<string, object>;
            Assert.Equal("dnd", notifyProperties["status"]);

            connProxy = default;
            await this.presenceService.UpdatePropertiesAsync(AsContactRef("conn3", "contact2"), new Dictionary<string, object>()
            {
                { "status", "away" },
            }, CancellationToken.None);
            Assert.Null(connProxy.Item1);

        }

        [Fact]
        public async Task SendMessageToConnection()
        {
            (string, object[]) conn2Proxy = default;
            this.clientProxies.Add("conn2", MockUtils.CreateClientProxy((m, args) =>
            {
                conn2Proxy = (m, args);
                return Task.CompletedTask;
            }));

            (string, object[]) conn3Proxy = default;
            this.clientProxies.Add("conn3", MockUtils.CreateClientProxy((m, args) =>
            {
                conn3Proxy = (m, args);
                return Task.CompletedTask;
            }));

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), null, CancellationToken.None);
            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn2", "contact2"), null, CancellationToken.None);
            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn3", "contact2"), null, CancellationToken.None);

            conn2Proxy = default;
            conn3Proxy = default;

            await this.presenceService.SendMessageAsync(
                AsContactRef("conn1", "contact1"),
                AsContactRef("conn2", "contact2"),
                "raw",
                100,
                CancellationToken.None);
            Assert.NotNull(conn2Proxy.Item1);
            Assert.Null(conn3Proxy.Item1);

            conn2Proxy = default;
            conn3Proxy = default;

            await this.presenceService.SendMessageAsync(
                AsContactRef("conn1", "contact1"),
                AsContactRef("conn3", "contact2"),
                "raw",
                100,
                CancellationToken.None);
            Assert.Null(conn2Proxy.Item1);
            Assert.NotNull(conn3Proxy.Item1);
        }

        [Fact]
        public async Task MultipleConnectionSubscription()
        {
            var connProxies = new List<(string, object[])>();
            this.clientProxies.Add("conn1", MockUtils.CreateClientProxy((m, args) =>
            {
                connProxies.Add((m, args));
                return Task.CompletedTask;
            }));

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn1", "contact1"), null, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn3", "contact2"), new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);

            var contactProperties = await this.presenceService.AddSubcriptionsAsync(
               AsContactRef("conn1", "contact1"),
               new ContactReference[] { AsContactRef(null, "contact2") }, new string[] { "status" });

            contactProperties = await this.presenceService.AddSubcriptionsAsync(
                AsContactRef("conn1", "contact1"),
                new ContactReference[] { AsContactRef("conn2", "contact2") }, new string[] { "status" });

            contactProperties = await this.presenceService.AddSubcriptionsAsync(
                AsContactRef("conn1", "contact1"),
                new ContactReference[] { AsContactRef("conn3", "contact2") }, new string[] { "status" });


            connProxies.Clear();
            await this.presenceService.UpdatePropertiesAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "status", "dnd" },
            }, CancellationToken.None);
            Assert.Equal(2, connProxies.Count);
            var conn2Notify = connProxies.FirstOrDefault(t => t.Item2[2]?.Equals("conn2") == true);
            Assert.NotNull(conn2Notify.Item1);

            var connNullNotify = connProxies.FirstOrDefault(t => t.Item2[2] == null);
            Assert.NotNull(conn2Notify.Item1);

            var notifyProperties = conn2Notify.Item2[1] as Dictionary<string, object>;
            Assert.Equal("dnd", notifyProperties["status"]);

            connProxies.Clear();
            await this.presenceService.UpdatePropertiesAsync(AsContactRef("conn3", "contact2"), new Dictionary<string, object>()
            {
                { "status", "away" },
            }, CancellationToken.None);
            Assert.Equal(2, connProxies.Count);

            var conn3Notify = connProxies.FirstOrDefault(t => t.Item2[2]?.Equals("conn3") == true);
            Assert.NotNull(conn3Notify.Item1);

            connNullNotify = connProxies.FirstOrDefault(t => t.Item2[2] == null);
            Assert.NotNull(conn2Notify.Item1);

            notifyProperties = connProxies[0].Item2[1] as Dictionary<string, object>;
            Assert.Equal("away", notifyProperties["status"]);
        }

        [Fact]
        public async Task SelfSubscription()
        {
            var connProxies = new List<(string, object[])>();
            this.clientProxies.Add("conn1", MockUtils.CreateClientProxy((m, args) =>
            {
                connProxies.Add((m, args));
                return Task.CompletedTask;
            }));

            var contact1Ref = AsContactRef("conn1", "contact1");
            await this.presenceService.RegisterSelfContactAsync(contact1Ref, new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);


            await this.presenceService.AddSubcriptionsAsync(
                contact1Ref,
                new ContactReference[] { AsContactRef(null, "contact1") }, new string[] { "status" });

            connProxies.Clear();
            await this.presenceService.UpdatePropertiesAsync(contact1Ref, new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);
            Assert.Single(connProxies);
        }

        [Fact]
        public async Task AllProperties()
        {
            var conn1Proxy = new Dictionary<string, object[]>();
            this.clientProxies.Add("conn1", MockUtils.CreateClientProxy((m, args) =>
            {
                conn1Proxy[m] = args;
                return Task.CompletedTask;
            }));

            var contact1Ref = AsContactRef("conn1", "contact1");
            var contact2Ref = AsContactRef("conn2", "contact2");
            await this.presenceService.RegisterSelfContactAsync(contact1Ref, null, CancellationToken.None);
            await this.presenceService.RegisterSelfContactAsync(contact2Ref, new Dictionary<string, object>()
            {
                { "property0", 10 },
            }, CancellationToken.None);

            var result = await this.presenceService.AddSubcriptionsAsync(
                contact1Ref,
                new ContactReference[] { contact2Ref }, new string[] { "*" });

            var resultProperties = result[contact2Ref.Id];
            Assert.Equal(10, resultProperties["property0"]);

            conn1Proxy.Clear();
            await this.presenceService.UpdatePropertiesAsync(contact2Ref, new Dictionary<string, object>()
            {
                { "property1", 100 },
                { "property2", "hello" },
            }, CancellationToken.None);

            Assert.True(conn1Proxy.ContainsKey(PresenceHubMethods.UpdateValues));
            var notifyProperties = conn1Proxy[PresenceHubMethods.UpdateValues][1] as Dictionary<string, object>;
            Assert.Equal(2, notifyProperties.Count);
            Assert.Equal(100, notifyProperties["property1"]);
            Assert.Equal("hello", notifyProperties["property2"]);

            conn1Proxy.Clear();
            await this.presenceService.UpdatePropertiesAsync(contact2Ref, new Dictionary<string, object>()
            {
                { "property3", true },
            }, CancellationToken.None);
            notifyProperties = conn1Proxy[PresenceHubMethods.UpdateValues][1] as Dictionary<string, object>;
            Assert.Single(notifyProperties);
            Assert.Equal(true, notifyProperties["property3"]);
        }


        private static ContactReference AsContactRef(string connectionId, string id ) => new ContactReference(id, connectionId);
        private static void AssertContactRef(string connectionId, string id, ContactReference contactReference)
        {
            Assert.Equal(AsContactRef(connectionId, id), contactReference);
        }

        private static void AssertContactRef(string connectionId, string id, object contactReference)
        {
            Assert.IsType<ContactReference>(contactReference);
            AssertContactRef(connectionId, id, (ContactReference)contactReference);
        }

        private class CustomMatchService : PresenceService
        {
            public CustomMatchService(IEnumerable<IHubContextHost> hubContextHosts, ILogger<PresenceService> logger)
                : base(new PresenceServiceOptions(), hubContextHosts, logger)
            {
            }

            public override Task<Dictionary<string, Dictionary<string, object>>[]> MatchContactsAsync(Dictionary<string, object>[] matchingProperties, CancellationToken cancellationToken)
            {
                var results = new Dictionary<string, Dictionary<string, object>>[matchingProperties.Length];
                for (int index = 0;index < matchingProperties.Length; ++index)
                {
                    var item = matchingProperties[index];
                    var result = new Dictionary<string, Dictionary<string, object>>();
                    if (item.TryGetValue("email", out var email) && email.Equals("contact4-alternate@microsoft.com"))
                    {
                        result.Add("contact4", null);
                    }

                    results[index] = result;
                }

                return Task.FromResult(results);
            }
        }
    }
}
