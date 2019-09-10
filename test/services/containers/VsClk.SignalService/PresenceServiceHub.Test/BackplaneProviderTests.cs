using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.PresenceServiceHubTests
{
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;
    using ConnectionProperties = IDictionary<string, PropertyValue>;

    public abstract class BackplaneProviderTests : TestBase, IAsyncLifetime
    {
        private IBackplaneProvider backplaneProvider;

        public async Task InitializeAsync()
        {
            this.backplaneProvider = await CreateBackplaneProviderAsync();
            await this.backplaneProvider.UpdateMetricsAsync("serviceId", null, default, default);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        protected abstract Task<IBackplaneProvider> CreateBackplaneProviderAsync();

        protected async Task TestUpdateContactsInternal()
        {
            var callbackCompleted = new VisualStudio.Threading.AsyncManualResetEvent();
            ContactDataChanged<ContactDataInfo> callbackContactDataChanged = null;
            string[] callbackAffectedProperties = null;
            OnContactChangedAsync onContactsChanged = (contactDataChanged,
                affectedProperties,
                cancellationToken) =>
            {
                callbackContactDataChanged = contactDataChanged;
                callbackAffectedProperties = affectedProperties;
                callbackCompleted.Set();
                return Task.CompletedTask;
            };
            this.backplaneProvider.ContactChangedAsync = onContactsChanged;

            var utcNow = DateTime.UtcNow;
            var properties = new Dictionary<string, PropertyValue>()
            {
                { "email", new PropertyValue("contact1@microsoft.com", utcNow) },
                { "status", new PropertyValue("available", utcNow) },
            };

            await this.backplaneProvider.UpdateContactAsync(
                new ContactDataChanged<ConnectionProperties >(
                    CreateChangeId(),
                    "serviceId",
                    "conn1",
                    "contact1",
                    ContactUpdateType.Registration,
                    properties),
                    CancellationToken.None);
            await callbackCompleted.WaitAsync();

            Assert.Equal("serviceId", callbackContactDataChanged.ServiceId);
            Assert.Equal("conn1", callbackContactDataChanged.ConnectionId);
            Assert.Equal("contact1", callbackContactDataChanged.ContactId);
            Assert.Equal(ContactUpdateType.Registration, callbackContactDataChanged.Type);
            Assert.Equal("available", callbackContactDataChanged.Data.GetAggregatedProperties()["status"]);
            Assert.Contains("email", callbackAffectedProperties);
            Assert.Contains("status", callbackAffectedProperties);

            callbackCompleted.Reset();

            properties["status"] = new PropertyValue("busy", DateTime.UtcNow);
            await this.backplaneProvider.UpdateContactAsync(
                new ContactDataChanged<ConnectionProperties>(
                    CreateChangeId(),
                    "serviceId",
                    "conn1",
                    "contact1",
                    ContactUpdateType.UpdateProperties,
                    properties),
                    CancellationToken.None);

            await callbackCompleted.WaitAsync();
            Assert.Equal("serviceId", callbackContactDataChanged.ServiceId);
            Assert.Equal("conn1", callbackContactDataChanged.ConnectionId);
            Assert.Equal("contact1", callbackContactDataChanged.ContactId);
            Assert.Equal(ContactUpdateType.UpdateProperties, callbackContactDataChanged.Type);
            Assert.Equal("busy", callbackContactDataChanged.Data.GetAggregatedProperties()["status"]);

            await this.backplaneProvider.UpdateContactAsync(
                new ContactDataChanged<ConnectionProperties>(
                    CreateChangeId(),
                    "serviceId",
                    "conn2",
                    "contact1",
                    ContactUpdateType.Registration,
                    new Dictionary<string, PropertyValue>()
                    {
                        { "other", new PropertyValue(100, DateTime.UtcNow) },
                    }),
                    CancellationToken.None);

            var contactData = await this.backplaneProvider.GetContactDataAsync("contact1", CancellationToken.None);
            Assert.NotNull(contactData);
            Assert.True(contactData.ContainsKey("serviceId"));
            Assert.Equal(2, contactData["serviceId"].Count);
            Assert.True(contactData["serviceId"].ContainsKey("conn1"));
            Assert.True(contactData["serviceId"].ContainsKey("conn2"));
            Assert.True(contactData["serviceId"]["conn2"].ContainsKey("other"));
        }

        protected async Task TestSendMessageInternal()
        {
            var callbackCompleted = new VisualStudio.Threading.AsyncManualResetEvent();
            string callbackSourceId = null;
            MessageData callbackMessageData = null;
            OnMessageReceivedAsync onMessageReceived = (
                string sourceId,
                MessageData messageData,
                CancellationToken cancellationToken) =>
            {
                callbackSourceId = sourceId;
                callbackMessageData = messageData;
                callbackCompleted.Set();
                return Task.CompletedTask;
            };

            this.backplaneProvider.MessageReceivedAsync = onMessageReceived;

            var messageData = new MessageData(
                Guid.NewGuid().ToString(),
                AsContactRef("conn1", "contact1"),
                AsContactRef(null, "contact2"),
                "typeTest",
                100);
            await this.backplaneProvider.SendMessageAsync(
                "serviceId",
                messageData,
                CancellationToken.None);
            await callbackCompleted.WaitAsync();
            Assert.Equal("serviceId", callbackSourceId);
            Assert.NotNull(callbackMessageData);
            Assert.Equal(messageData.ChangeId, callbackMessageData.ChangeId);
            AssertContactRef(null, "contact1", callbackMessageData.FromContact);
            AssertContactRef(null, "contact2", callbackMessageData.TargetContact);
            Assert.Equal("typeTest", callbackMessageData.Type);
            Assert.Equal(JToken.FromObject(100), callbackMessageData.Body);
        }

        protected async Task GetContactsTestInternal()
        {
            var utcNow = DateTime.UtcNow;
            var properties1 = new Dictionary<string, PropertyValue>()
            {
                { "email", new PropertyValue("contact1@microsoft.com", utcNow) },
                { "status", new PropertyValue("available", utcNow) },
            };

            await this.backplaneProvider.UpdateContactAsync(
                new ContactDataChanged<ConnectionProperties>(
                    CreateChangeId(),
                    "serviceId",
                    "conn1",
                    "contact1",
                    ContactUpdateType.Registration,
                    properties1),
                    CancellationToken.None);

            utcNow = DateTime.UtcNow;
            var properties2 = new Dictionary<string, PropertyValue>()
            {
                { "email", new PropertyValue("contact2@microsoft.com", utcNow) },
                { "status", new PropertyValue("available", utcNow) },
            };

            await this.backplaneProvider.UpdateContactAsync(
                new ContactDataChanged<ConnectionProperties>(
                    CreateChangeId(),
                    "serviceId",
                    "conn2",
                    "contact2",
                    ContactUpdateType.Registration,
                    properties2),
                    CancellationToken.None);

            var results = await this.backplaneProvider.GetContactsDataAsync(new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
            }, CancellationToken.None);
            Assert.Single(results);
            Assert.True(results.ContainsKey("contact2"));

            results = await this.backplaneProvider.GetContactsDataAsync(new Dictionary<string, object>()
            {
                { "email", "unknown@microsoft.com" },
            }, CancellationToken.None);
            Assert.Empty(results);
        }
    }
}

