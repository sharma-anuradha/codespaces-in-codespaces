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

        public PresenceServiceHubTests()
        {
            this.clientProxies = new Dictionary<string, IClientProxy>();
            var serviceLogger = new Mock<ILogger<PresenceService>>();

            var trace = new TraceSource("PresenceServiceHubTests");
            this.presenceService = new PresenceService(MockUtils.CreateHubContextMock(this.clientProxies), serviceLogger.Object);
        }

        [Fact]
        public async Task RegisterTest()
        {
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

            await this.presenceService.RegisterSelfContactAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
                { "other", 100 },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync("conn2", "contact2", new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            var contactProperties = await this.presenceService.AddSubcriptionsAsync("conn2", "contact2", new string[] { "contact1" }, new string[] { "status", "other" });
            Assert.NotNull(contactProperties);
            Assert.Single(contactProperties);
            Assert.Single(contactProperties);

            var contact1PropertiesExpected = new Dictionary<string, object>()
            {
                { "status", "available" },
                { "other", 100 },
            };

            contactProperties["contact1"].All(e => contact1PropertiesExpected.Contains(e));

            await this.presenceService.UpdatePropertiesAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);

            Assert.Equal(conn1Proxy.Item1, Methods.UpdateValues);
            Assert.Equal(conn2Proxy.Item1, Methods.UpdateValues);
            Assert.Equal(2, conn2Proxy.Item2.Length);
            Assert.Equal("contact1", conn2Proxy.Item2[0]);
            var notifyProperties = conn2Proxy.Item2[1] as Dictionary<string, object>;

            this.presenceService.RemoveSubcriptionProperties("conn2", "contact2", new string[] { "contact1" }, new string[] { "status" });
            conn2Proxy = default;
            await this.presenceService.UpdatePropertiesAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);

            Assert.Null(conn2Proxy.Item1);
            await this.presenceService.UpdatePropertiesAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "other", 200 },
            }, CancellationToken.None);
            Assert.Equal(conn2Proxy.Item1, Methods.UpdateValues);

            this.presenceService.RemoveSubscription("conn2", "contact2", new string[] { "contact1" });
            conn2Proxy = default;
            await this.presenceService.UpdatePropertiesAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "status", "busy" },
                { "other", 300 },
            }, CancellationToken.None);
            Assert.Null(conn2Proxy.Item1);

            conn2Proxy = default;
            await this.presenceService.AddSubcriptionsAsync("conn2", "contact2", new string[] { "contact1" }, new string[] { "status", "other" });

            // unregister with reconnection should not affect "conn2"
            await this.presenceService.UnregisterSelfContactAsync("conn1", "contact1", async (p) =>
            {
                // before removing we will add a new connection
                await this.presenceService.RegisterSelfContactAsync("conn3", "contact1", new Dictionary<string, object>()
                {
                    { "email", "contact1@microsoft.com" },
                    { "status", "busy" },
                    { "other", 300 },
                }, CancellationToken.None);
                conn2Proxy = default;
            }, CancellationToken.None);
            await this.presenceService.UnregisterSelfContactAsync("conn1", "contact1", null, CancellationToken.None);
            Assert.Null(conn2Proxy.Item1);

            // clear 'conn3'
            await this.presenceService.UnregisterSelfContactAsync("conn3", "contact1", null, CancellationToken.None);
            // publish 'contact1' & 'conn1'
            await this.presenceService.RegisterSelfContactAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
                { "other", 100 },
            }, CancellationToken.None);
            conn2Proxy = default;

            await this.presenceService.UnregisterSelfContactAsync("conn1", "contact1", null, CancellationToken.None);
            Assert.Equal(conn2Proxy.Item1, Methods.UpdateValues);
            Assert.Equal(2, conn2Proxy.Item2.Length);
            Assert.Equal("contact1", conn2Proxy.Item2[0]);
            notifyProperties = (Dictionary<string, object>)conn2Proxy.Item2[1];
            Assert.Null(notifyProperties["other"]);
            Assert.Null(notifyProperties["status"]);
        }

        [Fact]
        public async Task TwoConnectionsTest()
        {
            await this.presenceService.RegisterSelfContactAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "value", 100 },
            }, CancellationToken.None);
            await this.presenceService.RegisterSelfContactAsync("conn2", "contact1", new Dictionary<string, object>()
            {
                { "value", 200 },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync("conn3", "contact2", null, CancellationToken.None);
            var subscriptionsResults = await this.presenceService.AddSubcriptionsAsync("conn3", "contact2", new string[] { "contact1" }, new string[] { "value" });
            Assert.Equal(200, subscriptionsResults["contact1"]["value"]);

            this.presenceService.RemoveSubscription("conn3", "contact2", new string[] { "contact1" });
            await this.presenceService.UnregisterSelfContactAsync("conn2", "contact1", null, CancellationToken.None);
            subscriptionsResults = await this.presenceService.AddSubcriptionsAsync("conn3", "contact2", new string[] { "contact1" }, new string[] { "value" });
            Assert.Equal(100, subscriptionsResults["contact1"]["value"]);
        }

        [Fact]
        public async Task MatchMultipleContactsTest()
        {
            await this.presenceService.RegisterSelfContactAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync("conn2", "contact2", new Dictionary<string, object>()
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
            await this.presenceService.RegisterSelfContactAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "name", "Contact1" },
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync("conn2", "contact2", new Dictionary<string, object>()
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
            await this.presenceService.RegisterSelfContactAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync("conn2", "contact2", new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "busy" },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync("conn3", "contact3", new Dictionary<string, object>()
            {
                { "email", "contact3@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            await this.presenceService.RegisterSelfContactAsync("conn4", "contact4", new Dictionary<string, object>()
            {
                { "email", "contact4@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            var results = await this.presenceService.RequestSubcriptionsAsync("conn1", "contact1", new Dictionary<string, object>[] {
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
            var customMatchService = new CustomMatchService(this.presenceService.Hub, this.presenceService.Logger);

            await customMatchService.RegisterSelfContactAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            await customMatchService.RegisterSelfContactAsync("conn2", "contact2", new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "busy" },
            }, CancellationToken.None);

            await customMatchService.RegisterSelfContactAsync("conn3", "contact3", new Dictionary<string, object>()
            {
                { "email", "contact3@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            await customMatchService.RegisterSelfContactAsync("conn4", "contact4", new Dictionary<string, object>()
            {
                { "email", "contact4@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            var results = await customMatchService.RequestSubcriptionsAsync("conn1", "contact1", new Dictionary<string, object>[] {
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
            await this.presenceService.RegisterSelfContactAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            var results = await this.presenceService.RequestSubcriptionsAsync("conn1", "contact1", new Dictionary<string, object>[] {
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

            await this.presenceService.RegisterSelfContactAsync("conn2", "contact2", new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "busy" },
            }, CancellationToken.None);

            Assert.NotNull(conn1Proxy.Item1);
            Assert.Equal(conn1Proxy.Item1, Methods.UpdateValues);
            Assert.Equal(conn1Proxy.Item2[0], results[0][Properties.IdReserved]);
            conn1Proxy = default;
            await this.presenceService.UpdatePropertiesAsync("conn2", "contact2", new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);
            Assert.NotNull(conn1Proxy.Item1);
            Assert.Equal(conn1Proxy.Item1, Methods.UpdateValues);
            Assert.Equal(conn1Proxy.Item2[0], results[0][Properties.IdReserved]);

            await this.presenceService.SendMessageAsync("contact1", results[0][Properties.IdReserved].ToString(), "raw", 100, CancellationToken.None);
            Assert.NotNull(conn2Proxy.Item1);
            Assert.Equal(conn2Proxy.Item1, Methods.ReceiveMessage);
            Assert.Equal("contact2", conn2Proxy.Item2[0]);
            Assert.Equal("contact1", conn2Proxy.Item2[1]);

            this.presenceService.RemoveSubscription("conn1", "contact1", new string[] { results[0][Properties.IdReserved].ToString() });
            results = await this.presenceService.RequestSubcriptionsAsync("conn1", "contact1", new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact2@microsoft.com" },
                }},
                new string[] { "status" }, useStubContact: true, CancellationToken.None);
            Assert.Single(results);
            Assert.Equal("contact2", results[0][Properties.IdReserved]);
        }

        private class CustomMatchService : PresenceService
        {
            public CustomMatchService(IHubContext<PresenceServiceHub> hub, ILogger<PresenceService> logger)
                : base(hub, logger)
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
