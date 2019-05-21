//#define _HAS_AZURE_COSMOS_EMULATOR

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.PresenceServiceHubTests
{
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
            string callbackContactId = null;
            string callbackConnectionId = null;
            Dictionary<string, object> callbackProperties = null;
            ContactUpdateType callbackUpdateContactType = ContactUpdateType.None;

            OnContactChangedAsync onContactsChanged = (
                    sourceId,
                    connectionId,
                    contactData,
                    updateContactType,
                    cancellationToken) =>
            {
                callbackConnectionId = connectionId;
                callbackContactId = contactData.Id;
                callbackProperties = contactData.Properties;
                callbackUpdateContactType = updateContactType;

                callbackCompleted.Set();
                return Task.CompletedTask;
            };
            this.databaseBackplaneProvider.ContactChangedAsync = onContactsChanged;

            var properties = new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            };
            var connections = new Dictionary<string, Dictionary<string, object>>()
            {
                { "conn1", properties },
            };

            await this.databaseBackplaneProvider.UpdateContactAsync(
                "sourceId",
                "conn1",
                new ContactData("contact1", properties, connections),
                ContactUpdateType.Registration,
                CancellationToken.None);
            await callbackCompleted.WaitAsync();

            Assert.Equal("conn1", callbackConnectionId);
            Assert.Equal("contact1", callbackContactId);
            Assert.Equal(ContactUpdateType.Registration, callbackUpdateContactType);
            Assert.Equal("available", callbackProperties["status"]);

            callbackCompleted.Reset();

            properties["status"] = "busy";
            await this.databaseBackplaneProvider.UpdateContactAsync(
                "sourceId",
                "conn1",
                new ContactData("contact1", properties, connections),
                ContactUpdateType.UpdateProperties,
                CancellationToken.None);
            await callbackCompleted.WaitAsync();
            Assert.Equal("contact1", callbackContactId);
            Assert.Equal(ContactUpdateType.UpdateProperties, callbackUpdateContactType);
            Assert.Equal("busy", callbackProperties["status"]);
        }

#if _HAS_AZURE_COSMOS_EMULATOR
        [Fact]
#else
        [Fact(Skip = "Require Azure Cosmos Emulator")]
#endif
        public async Task GetContactsTest()
        {
            var properties1 = new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            };
            var connections1 = new Dictionary<string, Dictionary<string, object>>()
            {
                { "conn1", properties1 },
            };

            await this.databaseBackplaneProvider.UpdateContactAsync(
                "sourceId",
                "conn1",
                new ContactData("contact1", properties1, connections1),
                ContactUpdateType.Registration,
                CancellationToken.None);

            var properties2 = new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "available" },
            };
            var connections2 = new Dictionary<string, Dictionary<string, object>>()
            {
                { "conn2", properties2 },
            };

            await this.databaseBackplaneProvider.UpdateContactAsync(
                "sourceId",
                "conn1",
                new ContactData("contact2", properties2, connections2),
                ContactUpdateType.Registration,
                CancellationToken.None);

            var results = await this.databaseBackplaneProvider.GetContactsAsync(new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
            }, CancellationToken.None);
            Assert.Single(results);
            Assert.Equal("contact2", results[0].Properties[Properties.IdReserved]);

            results = await this.databaseBackplaneProvider.GetContactsAsync(new Dictionary<string, object>()
            {
                { "email", "unknown@microsoft.com" },
            }, CancellationToken.None);
            Assert.Empty(results);
        }
    }
}
