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

    public class AzureRedisContactsProviderTests : ContactBackplaneProviderTests
    {
        protected override async Task<IContactBackplaneProvider> CreateBackplaneProviderAsync()
        {
            var connection = await ConnectionMultiplexer.ConnectAsync("localhost");

            return await AzureRedisContactsProvider.CreateAsync(
                (Guid.NewGuid().ToString(), "usw2"),
                new RedisConnectionPool(new ConnectionMultiplexer[] { connection }),
                new Mock<ILogger<AzureRedisContactsProvider>>().Object,
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
