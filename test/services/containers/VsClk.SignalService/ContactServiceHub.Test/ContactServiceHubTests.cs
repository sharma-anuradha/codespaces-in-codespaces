using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.ServiceHubTests;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VsCloudKernel.SignalService.PresenceServiceHubTests
{
    public class ContactServiceHubTests : TestBase
    {
        private readonly Dictionary<string, IClientProxy> clientProxies;
        private readonly ContactService presenceService;
        private readonly ILogger<ContactService> presenceServiceLogger;
        private readonly ITestOutputHelper output;

        public ContactServiceHubTests(ITestOutputHelper output)
        {
            this.output = output;
            this.clientProxies = new Dictionary<string, IClientProxy>();
            this.presenceServiceLogger = new Mock<ILogger<ContactService>>().Object;

            var trace = new TraceSource("PresenceServiceHubTests");
            this.presenceService = new ContactService(
                new HubServiceOptions() { Id = "mock" },
                MockUtils.CreateSingleHubContextHostMock<ContactServiceHub>(this.clientProxies),
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

            Assert.True(conn1Proxy.ContainsKey(ContactHubMethods.UpdateValues));
            Assert.Equal(3, conn2Proxy[ContactHubMethods.UpdateValues].Length);
            AssertContactRef("conn1", "contact1", conn2Proxy[ContactHubMethods.UpdateValues][0]);
            var notifyProperties = conn2Proxy[ContactHubMethods.UpdateValues][1] as Dictionary<string, object>;

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
            Assert.True(conn2Proxy.ContainsKey(ContactHubMethods.UpdateValues));

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
            Assert.True(conn2Proxy.ContainsKey(ContactHubMethods.ConnectionChanged));

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
            Assert.True(conn2Proxy.ContainsKey(ContactHubMethods.UpdateValues));
            Assert.Equal(3, conn2Proxy[ContactHubMethods.UpdateValues].Length);
            AssertContactRef("conn1", "contact1", conn2Proxy[ContactHubMethods.UpdateValues][0]);
            notifyProperties = (Dictionary<string, object>)conn2Proxy[ContactHubMethods.UpdateValues][1];
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
            Assert.Equal(200, Convert.ToInt32(subscriptionsResults["contact1"]["value"]));

            this.presenceService.RemoveSubscription(AsContactRef("conn3", "contact2"), new ContactReference[] { AsContactRef(null, "contact1") });
            await this.presenceService.UnregisterSelfContactAsync(AsContactRef("conn2", "contact1"), null, CancellationToken.None);
            subscriptionsResults = await this.presenceService.AddSubcriptionsAsync(AsContactRef("conn3", "contact2"), new ContactReference[] { AsContactRef(null, "contact1") }, new string[] { "value" });
            Assert.Equal(100, Convert.ToInt32(subscriptionsResults["contact1"]["value"]));
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
                    { ContactProperties.IdReserved, "contact4" },
                } },
                new string[] { "status" }, false, CancellationToken.None);
            Assert.Equal(4, results.Length);
            Assert.NotNull(results[0]);
            Assert.NotNull(results[2]);
            Assert.NotNull(results[3]);
            Assert.Null(results[1]);
            Assert.Equal("contact2", results[0][ContactProperties.IdReserved]);
            Assert.Equal("busy", results[0]["status"]);
            Assert.Equal("contact3", results[2][ContactProperties.IdReserved]);
            Assert.Equal("available", results[2]["status"]);
            Assert.Equal("contact4", results[3][ContactProperties.IdReserved]);
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
            Assert.Equal("contact4", results[1][ContactProperties.IdReserved]);
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
            Assert.Equal(conn1Proxy.Item1, ContactHubMethods.UpdateValues);
            AssertContactRef("conn2", results[0][ContactProperties.IdReserved].ToString(), conn1Proxy.Item2[0]);
            conn1Proxy = default;
            await this.presenceService.UpdatePropertiesAsync(AsContactRef("conn2", "contact2"), new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);
            Assert.NotNull(conn1Proxy.Item1);
            Assert.Equal(conn1Proxy.Item1, ContactHubMethods.UpdateValues);
            AssertContactRef("conn2", results[0][ContactProperties.IdReserved].ToString(),conn1Proxy.Item2[0]);

            await this.presenceService.SendMessageAsync(AsContactRef("conn1", "contact1"), AsContactRef(null, results[0][ContactProperties.IdReserved].ToString()), "raw", 100, CancellationToken.None);
            Assert.NotNull(conn2Proxy.Item1);
            Assert.Equal(conn2Proxy.Item1, ContactHubMethods.ReceiveMessage);
            AssertContactRef("conn2", "contact2", conn2Proxy.Item2[0]);
            AssertContactRef("conn1", "contact1", conn2Proxy.Item2[1]);

            this.presenceService.RemoveSubscription(AsContactRef("conn1", "contact1"), new ContactReference[] { AsContactRef(null, results[0][ContactProperties.IdReserved].ToString()) });
            results = await this.presenceService.RequestSubcriptionsAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact2@microsoft.com" },
                }},
                new string[] { "status" }, useStubContact: true, CancellationToken.None);
            Assert.Single(results);
            Assert.Equal("contact2", results[0][ContactProperties.IdReserved]);

            // now we ask for a more complex matching
            results = await this.presenceService.RequestSubcriptionsAsync(AsContactRef("conn1", "contact1"), new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact3@microsoft.com" },
                    { "name", "contact3" },
                }},
            new string[] { "status" }, useStubContact: true, CancellationToken.None);
            Assert.Single(results);
            Assert.NotNull(results[0]);
            Assert.Single(results[0]);
            Assert.True(results[0].ContainsKey(ContactProperties.IdReserved));

            conn1Proxy = default;
            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn3", "contact4"), new Dictionary<string, object>()
            {
                { "email", "contact3@microsoft.com" },
                { "status", "busy" },
            }, CancellationToken.None);

            // the last registration should NOT match
            Assert.Null(conn1Proxy.Item1);

            await this.presenceService.RegisterSelfContactAsync(AsContactRef("conn4", "contact3"), new Dictionary<string, object>()
            {
                { "email", "contact3@microsoft.com" },
                { "name", "contact3" },
                { "status", "busy" },
            }, CancellationToken.None);

            // this should match
            Assert.NotNull(conn1Proxy.Item1);
            Assert.Equal(conn1Proxy.Item1, ContactHubMethods.UpdateValues);
            AssertContactRef("conn4", results[0][ContactProperties.IdReserved].ToString(), conn1Proxy.Item2[0]);

            this.presenceService.RemoveSubscription(AsContactRef("conn1", "contact1"), new ContactReference[] { AsContactRef(null, results[0][ContactProperties.IdReserved].ToString()) });
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
            Assert.Equal(connProxy.Item1, ContactHubMethods.UpdateValues);
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
            Assert.Equal(10, Convert.ToInt32(resultProperties["property0"]));

            conn1Proxy.Clear();
            await this.presenceService.UpdatePropertiesAsync(contact2Ref, new Dictionary<string, object>()
            {
                { "property1", 100 },
                { "property2", "hello" },
            }, CancellationToken.None);

            Assert.True(conn1Proxy.ContainsKey(ContactHubMethods.UpdateValues));
            var notifyProperties = conn1Proxy[ContactHubMethods.UpdateValues][1] as Dictionary<string, object>;
            Assert.Equal(2, notifyProperties.Count);
            Assert.Equal(100, Convert.ToInt32(notifyProperties["property1"]));
            Assert.Equal("hello", notifyProperties["property2"]);

            conn1Proxy.Clear();
            await this.presenceService.UpdatePropertiesAsync(contact2Ref, new Dictionary<string, object>()
            {
                { "property3", true },
            }, CancellationToken.None);
            notifyProperties = conn1Proxy[ContactHubMethods.UpdateValues][1] as Dictionary<string, object>;
            Assert.Single(notifyProperties);
            Assert.Equal(true, notifyProperties["property3"]);
        }

        [Fact]
        public async Task PurgeTest()
        {
            var contact1Ref = AsContactRef("conn1", "contact1");
            var contact2Ref = AsContactRef("conn2", "contact2");

            var contact1Props = new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            };
            var contact2Props = new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "available" },
            };

            await this.presenceService.RegisterSelfContactAsync(contact1Ref, contact1Props, CancellationToken.None);
            
            var metrics = this.presenceService.GetMetrics();
            Assert.Equal(1, metrics.Count);
            await this.presenceService.UnregisterSelfContactAsync(contact1Ref, null, CancellationToken.None);
            metrics = this.presenceService.GetMetrics();
            Assert.Equal(0, metrics.Count);

            await this.presenceService.RegisterSelfContactAsync(contact1Ref, contact1Props, CancellationToken.None);
            await this.presenceService.AddSubcriptionsAsync(
                contact1Ref,
                new ContactReference[] { contact2Ref },
                new string[] { "*" });
            metrics = this.presenceService.GetMetrics();
            Assert.Equal(2, metrics.Count);
            this.presenceService.RemoveSubscription(
                contact1Ref,
                new ContactReference[] { contact2Ref });
            metrics = this.presenceService.GetMetrics();
            Assert.Equal(1, metrics.Count);
            await this.presenceService.AddSubcriptionsAsync(
                contact1Ref,
                new ContactReference[] { contact2Ref },
                new string[] { "*" });
            metrics = this.presenceService.GetMetrics();
            Assert.Equal(2, metrics.Count);
            await this.presenceService.RegisterSelfContactAsync(contact2Ref, contact2Props, CancellationToken.None);
            metrics = this.presenceService.GetMetrics();
            Assert.Equal(2, metrics.Count);
            this.presenceService.RemoveSubscription(
                contact1Ref,
                new ContactReference[] { contact2Ref });
            metrics = this.presenceService.GetMetrics();
            Assert.Equal(2, metrics.Count);
            await this.presenceService.UnregisterSelfContactAsync(contact1Ref, null, CancellationToken.None);
            await this.presenceService.UnregisterSelfContactAsync(contact2Ref, null, CancellationToken.None);
            metrics = this.presenceService.GetMetrics();
            Assert.Equal(0, metrics.Count);
        }

        [Fact]
        public async Task MemoryAndPerfTest()
        {
            const int NumberOfRegisteredContacts = 100000;
            const int NumberOfUnregisteredContacts = 100000;

            Action gcCollect = () =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            };

            var contact1Ref = AsContactRef("conn1", "contact1");
            await this.presenceService.RegisterSelfContactAsync(contact1Ref, new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            gcCollect();
            var memory1 = Common.LoggerScopeHelpers.GetProcessMemoryInfo();
            var registerSelfProperties = new Dictionary<string, object>()
            {
                { "property1", 100 },
                { "property2", "Value2" },
                { "status", "available" },
            };

            for (int index = 0; index < NumberOfRegisteredContacts; ++index)
            {
                var nextContactRef = AsContactRef($"conn1_registered_{index}", $"contact_registered_{index}");
                await this.presenceService.RegisterSelfContactAsync(contact1Ref, registerSelfProperties, default);
            }

            gcCollect();
            var memory2 = Common.LoggerScopeHelpers.GetProcessMemoryInfo();
            var totalMemoryOnRegisteredContacts = memory2.memorySize - memory1.memorySize;

            this.output.WriteLine($"Total memory in MB for registered contacts count:{NumberOfRegisteredContacts} total:{totalMemoryOnRegisteredContacts}");

            for (int index = 0; index < NumberOfUnregisteredContacts; ++index)
            {
                var nextContactRef = AsContactRef($"conn1_unregistered_{index}", $"contact_unregistered_{index}");
                await this.presenceService.AddSubcriptionsAsync(contact1Ref, new ContactReference[] { nextContactRef }, new string[] { "*" });
            }

            gcCollect();
            memory1 = Common.LoggerScopeHelpers.GetProcessMemoryInfo();
            var totalMemoryOnSubscriptionContacts = memory1.memorySize - memory2.memorySize;
            this.output.WriteLine($"Total memory in MB for empty subscription contacts count:{NumberOfUnregisteredContacts} total:{totalMemoryOnSubscriptionContacts}");

            var sw = Stopwatch.StartNew();
            await this.presenceService.RequestSubcriptionsAsync(
                contact1Ref,
                new Dictionary<string, object>[] {
                    new Dictionary<string, object>()
                    {
                        { "email", "contact2@microsoft.com" },
                    },
                },
                new string[] { "status" },
                useStubContact: false,
               default);
            var requestTimeElapsed = sw.ElapsedMilliseconds;
            this.output.WriteLine($"Total tiem in ms to request a subscription:{requestTimeElapsed}");
        }

        private class CustomMatchService : ContactService
        {
            public CustomMatchService(IEnumerable<IHubContextHost> hubContextHosts, ILogger<ContactService> logger)
                : base(new HubServiceOptions() { Id = "custom" }, hubContextHosts, logger)
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
