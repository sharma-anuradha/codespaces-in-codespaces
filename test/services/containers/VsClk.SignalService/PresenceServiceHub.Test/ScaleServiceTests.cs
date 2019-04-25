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

            await this.presenceService1.RegisterSelfContactAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);

            var contactProperties = await this.presenceService1.AddSubcriptionsAsync("conn1", "contact1", new string[] { "contact2" }, new string[] { "status" });
            Assert.Null(contactProperties["contact2"]["status"]);

            conn1Proxy = default;
            await this.presenceService2.RegisterSelfContactAsync("conn1", "contact2", new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);

            Assert.Equal("contact2", conn1Proxy.Item2[0]);
            var notifyProperties = conn1Proxy.Item2[1] as Dictionary<string, object>;
            Assert.Equal("available", notifyProperties["status"]);

            contactProperties = await this.presenceService2.AddSubcriptionsAsync("conn1", "contact2", new string[] { "contact1" }, new string[] { "status" });
            Assert.Equal("available", contactProperties["contact1"]["status"]);

            conn2Proxy = default;
            await this.presenceService1.UpdatePropertiesAsync("conn1", "contact1", new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);
            Assert.Equal("contact1", conn2Proxy.Item2[0]);
            notifyProperties = conn2Proxy.Item2[1] as Dictionary<string, object>;
            Assert.Equal("busy", notifyProperties["status"]);

            conn2Proxy = default;
            await this.presenceService1.SendMessageAsync("contact1", "contact2", "type1", JToken.FromObject(100), default);
            Assert.Equal("contact2", conn2Proxy.Item2[0]);
            Assert.Equal("contact1", conn2Proxy.Item2[1]);
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

            await this.presenceService1.RegisterSelfContactAsync("conn1", "contact1", new Dictionary<string, object>()
            {
               { "email", "contact1@microsoft.com" },
               { "status", "available" },
            }, CancellationToken.None);

            var results = await this.presenceService1.RequestSubcriptionsAsync("conn1", "contact1", new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact2@microsoft.com" },
                }},
                new string[] { "status" }, useStubContact: true, CancellationToken.None);

            conn1Proxy = default;
            await this.presenceService2.RegisterSelfContactAsync("conn1", "contact2", new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "available" },
            }, CancellationToken.None);

            Assert.Equal(results[0][Properties.IdReserved], conn1Proxy.Item2[0]);
            var notifyProperties = conn1Proxy.Item2[1] as Dictionary<string, object>;
            Assert.Equal("available", notifyProperties["status"]);

            conn1Proxy = default;
            await this.presenceService2.UpdatePropertiesAsync("conn1", "contact2", new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, CancellationToken.None);
            Assert.Equal(results[0][Properties.IdReserved], conn1Proxy.Item2[0]);
            notifyProperties = conn1Proxy.Item2[1] as Dictionary<string, object>;
            Assert.Equal("busy", notifyProperties["status"]);

            // this should remove the stub contact 
            this.presenceService1.RemoveSubscription("conn1", "contact1", new string[] { results[0][Properties.IdReserved].ToString() });
            results = await this.presenceService1.RequestSubcriptionsAsync("conn1", "contact1", new Dictionary<string, object>[] {
                new Dictionary<string, object>()
                {
                    { "email", "contact2@microsoft.com" },
                }},
                new string[] { "status" }, useStubContact: true, CancellationToken.None);

            Assert.Single(results);
            Assert.Equal("contact2", results[0][Properties.IdReserved]);
            Assert.Equal("busy", results[0]["status"]);

            conn1Proxy = default;
            await this.presenceService2.UpdatePropertiesAsync("conn1", "contact2", new Dictionary<string, object>()
            {
                { "status", "available" },
            }, CancellationToken.None);
            Assert.Equal("contact2", conn1Proxy.Item2[0]);
            notifyProperties = conn1Proxy.Item2[1] as Dictionary<string, object>;
            Assert.Equal("available", notifyProperties["status"]);
        }

        private class MockBackplaneProvider : IBackplaneProvider
        {
            private readonly Dictionary<string, Dictionary<string, object>> contactProperties;
            private readonly List<OnContactChangedAsync> contactChangedAsyncs = new List<OnContactChangedAsync>();
            private readonly List<OnMessageReceivedAsync> messageReceivedAsyncs = new List<OnMessageReceivedAsync>();

            internal MockBackplaneProvider()
            {
                this.contactProperties = new Dictionary<string, Dictionary<string, object>>();
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

            public Task<Dictionary<string, object>> GetContactPropertiesAsync(string contactId, CancellationToken cancellationToken)
            {
                if (this.contactProperties.TryGetValue(contactId, out var properties))
                {
                    return Task.FromResult(properties);
                }

                return Task.FromResult<Dictionary<string, object>>(null);
            }

            public async Task SendMessageAsync(string sourceId, string contactId, string targetContactId, string messageType, JToken body, CancellationToken cancellationToken)
            {
                await Task.WhenAll(this.messageReceivedAsyncs.Select(c => c.Invoke(sourceId, contactId, targetContactId, messageType, body, cancellationToken)));
            }

            public async Task UpdateContactAsync(string sourceId, string contactId,Dictionary<string, object> properties, ContactUpdateType updateContactType, CancellationToken cancellationToken)
            {
                this.contactProperties[contactId] = properties;

                await Task.WhenAll(this.contactChangedAsyncs.Select(c => c.Invoke(sourceId, contactId, properties, updateContactType, cancellationToken)));
            }

            public Task<Dictionary<string, object>[]> GetContactsAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken)
            {
                var matchContacts = this.contactProperties
                    .Where(kvp => matchProperties.MatchProperties(kvp.Value))
                    .Select(kvp =>
                    {
                        var properties = kvp.Value.ToDictionary(entry => entry.Key, entry => entry.Value);
                        properties[Properties.IdReserved] = kvp.Key;
                        return properties;
                    }).ToArray();

                return Task.FromResult(matchContacts);
            }
        }
    }
}
