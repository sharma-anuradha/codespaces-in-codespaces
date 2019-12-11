#define _HAS_AZURE_COSMOS_EMULATOR

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.PresenceServiceHubTests
{
#if _HAS_AZURE_COSMOS_EMULATOR

    public class AzureDocumentsProviderTests : BackplaneProviderTests
    {
        private const string Uri = "https://localhost:8081";
        private const string PrimaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        protected override async Task<IContactBackplaneProvider> CreateBackplaneProviderAsync()
        {
            return await AzureDocumentsProvider.CreateAsync(
                (Guid.NewGuid().ToString(), "usw2"),
                new DatabaseSettings()
                {
                    EndpointUrl = Uri,
                    AuthorizationKey = PrimaryKey
                },
                new Mock<ILogger<AzureDocumentsProvider>>().Object,
                null,
                true);
        }


        [Fact]
        public Task TestUpdateContacts()
        {
            return TestUpdateContactsInternal();
        }

        [Fact]
        public Task GetContactsTest()
        {
            return GetContactsTestInternal();
        }

        [Fact]
        public Task TestSendMessage()
        {
            return TestSendMessageInternal();
        }
    }
#endif
}

