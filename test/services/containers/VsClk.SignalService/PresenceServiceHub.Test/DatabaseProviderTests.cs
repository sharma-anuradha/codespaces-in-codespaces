//#define _HAS_AZURE_COSMOS_EMULATOR

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.PresenceServiceHubTests
{
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;
    using ConnectionProperties = IDictionary<string, PropertyValue>;

    public class DatabaseProviderTests : IAsyncLifetime
    {
        private const string Uri = "https://localhost:8081";
        private const string PrimaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        private DatabaseBackplaneProvider databaseBackplaneProvider;

        public async Task InitializeAsync()
        {
            this.databaseBackplaneProvider = await DatabaseBackplaneProvider.CreateAsync(
                new DatabaseSettings()
                {
                    EndpointUrl = Uri,
                    AuthorizationKey = PrimaryKey
                },
                new Mock<ILogger<DatabaseBackplaneProvider>>().Object,
                true);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

#if _HAS_AZURE_COSMOS_EMULATOR
        [Fact]
#else
        [Fact(Skip = "Require Azure Cosmos Emulator")]
#endif
        public async Task TestUpdateContacts()
        {
            var callbackCompleted = new VisualStudio.Threading.AsyncManualResetEvent();
            ContactDataChanged<ContactDataInfo> callbackContactDataChanged = null;

            OnContactChangedAsync onContactsChanged = (contactDataChanged,
                    cancellationToken) =>
            {
                callbackContactDataChanged = contactDataChanged;
                callbackCompleted.Set();
                return Task.CompletedTask;
            };
            this.databaseBackplaneProvider.ContactChangedAsync = onContactsChanged;

            var utcNow = DateTime.UtcNow;
            var properties = new Dictionary<string, PropertyValue>()
            {
                { "email", new PropertyValue("contact1@microsoft.com", utcNow) },
                { "status", new PropertyValue("available", utcNow) },
            };

            await this.databaseBackplaneProvider.UpdateContactAsync(
                new ContactDataChanged<ConnectionProperties >(
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

            callbackCompleted.Reset();

            properties["status"] = new PropertyValue("busy", DateTime.UtcNow);
            await this.databaseBackplaneProvider.UpdateContactAsync(
                new ContactDataChanged<ConnectionProperties>(
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

            await this.databaseBackplaneProvider.UpdateContactAsync(
                new ContactDataChanged<ConnectionProperties>(
                "serviceId",
                "conn2",
                "contact1",
                ContactUpdateType.Registration,
                new Dictionary<string, PropertyValue>()
                {
                    { "other", new PropertyValue(100, DateTime.UtcNow) },
                }),
                CancellationToken.None);

            var contactData = await this.databaseBackplaneProvider.GetContactDataAsync("contact1", CancellationToken.None);
            Assert.NotNull(contactData);
            Assert.True(contactData.ContainsKey("serviceId"));
            Assert.Equal(2, contactData["serviceId"].Count);
            Assert.True(contactData["serviceId"].ContainsKey("conn1"));
            Assert.True(contactData["serviceId"].ContainsKey("conn2"));
            Assert.True(contactData["serviceId"]["conn2"].ContainsKey("other"));
        }

#if _HAS_AZURE_COSMOS_EMULATOR
        [Fact]
#else
        [Fact(Skip = "Require Azure Cosmos Emulator")]
#endif
        public async Task GetContactsTest()
        {
            var utcNow = DateTime.UtcNow;
            var properties1 = new Dictionary<string, PropertyValue>()
            {
                { "email", new PropertyValue("contact1@microsoft.com", utcNow) },
                { "status", new PropertyValue("available", utcNow) },
            };

            await this.databaseBackplaneProvider.UpdateContactAsync(
                new ContactDataChanged<ConnectionProperties>(
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

            await this.databaseBackplaneProvider.UpdateContactAsync(
                new ContactDataChanged<ConnectionProperties>(
                "serviceId",
                "conn2",
                "contact2",
                ContactUpdateType.Registration,
                properties2),
                CancellationToken.None);

            var results = await this.databaseBackplaneProvider.GetContactsDataAsync(new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
            }, CancellationToken.None);
            Assert.Single(results);
            Assert.True(results.ContainsKey("contact2"));

            results = await this.databaseBackplaneProvider.GetContactsDataAsync(new Dictionary<string, object>()
            {
                { "email", "unknown@microsoft.com" },
            }, CancellationToken.None);
            Assert.Empty(results);
        }
    }
}

