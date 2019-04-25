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
            Dictionary<string, object> callbackProperties = null;
            ContactUpdateType callbackUpdateContactType = ContactUpdateType.None;

            OnContactChangedAsync onContactsChanged = (
                    sourceId,
                    contactId,
                    properties,
                    updateContactType,
                    cancellationToken) =>
            {
                callbackContactId = contactId;
                callbackProperties = properties;
                callbackUpdateContactType = updateContactType;

                callbackCompleted.Set();
                return Task.CompletedTask;
            };
            this.databaseBackplaneProvider.ContactChangedAsync = onContactsChanged;

            await this.databaseBackplaneProvider.UpdateContactAsync("sourceId", "contact1", new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            }, ContactUpdateType.Registration, CancellationToken.None);
            await callbackCompleted.WaitAsync();

            Assert.Equal("contact1", callbackContactId);
            Assert.Equal(ContactUpdateType.Registration, callbackUpdateContactType);
            Assert.Equal("available", callbackProperties["status"]);

            callbackCompleted.Reset();
            await this.databaseBackplaneProvider.UpdateContactAsync("sourceId", "contact1", new Dictionary<string, object>()
            {
                { "status", "busy" },
            }, ContactUpdateType.UpdateProperties, CancellationToken.None);
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
            await this.databaseBackplaneProvider.UpdateContactAsync("sourceId", "contact1", new Dictionary<string, object>()
            {
                { "email", "contact1@microsoft.com" },
                { "status", "available" },
            }, ContactUpdateType.Registration, CancellationToken.None);
            await this.databaseBackplaneProvider.UpdateContactAsync("sourceId", "contact2", new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
                { "status", "available" },
            }, ContactUpdateType.Registration, CancellationToken.None);

            var results = await this.databaseBackplaneProvider.GetContactsAsync(new Dictionary<string, object>()
            {
                { "email", "contact2@microsoft.com" },
            }, CancellationToken.None);
            Assert.Single(results);
            Assert.Equal("contact2", results[0][Properties.IdReserved]);

            results = await this.databaseBackplaneProvider.GetContactsAsync(new Dictionary<string, object>()
            {
                { "email", "unknown@microsoft.com" },
            }, CancellationToken.None);
            Assert.Empty(results);
        }
    }
}
