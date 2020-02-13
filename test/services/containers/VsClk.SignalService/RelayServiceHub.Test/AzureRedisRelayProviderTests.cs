using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Microsoft.VsCloudKernel.SignalService.RelayServiceHubTests
{
#if _IS_REDIS_SERVER_RUNNING
    public class AzureRedisRelayProviderTests : RelayBackplaneProviderTests
    {


        protected override async Task<IRelayBackplaneProvider> CreateBackplaneProviderAsync()
        {
            var connection = await ConnectionMultiplexer.ConnectAsync("localhost");

            return await AzureRedisRelayProvider.CreateAsync(
                (Guid.NewGuid().ToString(), "usw2"),
                new RedisConnectionPool(new ConnectionMultiplexer[] { connection }),
                new Mock<ILogger<AzureRedisRelayProvider>>().Object);
        }

        [Fact]
        public Task TestRelayParticipantChanged()
        {
            return TestRelayParticipantChangedInternal();
        }

        [Fact]
        public Task TestRelayInfo()
        {
            return TestRelayInfoInternal();
        }
    }
#endif
}
