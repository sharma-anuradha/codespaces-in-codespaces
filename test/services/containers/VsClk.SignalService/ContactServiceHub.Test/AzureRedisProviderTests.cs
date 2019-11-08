//#define _IS_REDIS_SERVER_RUNNING

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.PresenceServiceHubTests
{
#if _IS_REDIS_SERVER_RUNNING

    public class AzureRedisProviderTests : BackplaneProviderTests
    {
        protected override async Task<IBackplaneProvider> CreateBackplaneProviderAsync()
        {
            var connection = await ConnectionMultiplexer.ConnectAsync("localhost");

            return await AzureRedisProvider.CreateAsync(
                Guid.NewGuid().ToString(),
                connection,
                new Mock<ILogger<AzureRedisProvider>>().Object,
                null);
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
