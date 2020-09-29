
using System;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HeartBeat.Test
{
    public class EnvironmentHeartbeatManagerTests : MonitoringTestBase
    {
        [Fact]
        public async void ThrowInvalidOperationExceptionWhenWrongHandlerIsCalled()
        {
            var handlerContext = await GetHandlerContext();
            await Assert.ThrowsAsync<InvalidOperationException>(() => environmentSessionDataHandler.ProcessAsync(testEnvironmentData, handlerContext, vmResourceId, logger));
        }
    }
}
