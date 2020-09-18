using System.ComponentModel.DataAnnotations;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.HeartBeat.Test;

using System;
using Xunit;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace HeartBeat.Test
{
    public class EnvironmentSessionDataHandlerTests : MonitoringTestBase
    {
        [Fact]
        public async void ThrowInvalidOperationExceptionWhenWrongHandlerIsCalled()
        {
            var handlerContext = await GetHandlerContext();
            await Assert.ThrowsAsync<InvalidOperationException>(() => environmentSessionDataHandler.ProcessAsync(testEnvironmentData, handlerContext, vmResourceId, logger));
        }

        [Fact]
        public async void UpdateLastUsedWithActiveUsers()
        {
            var environmentSessionData = CreateEnvironmentSessionDataObject();
            var handlerContext = await GetHandlerContext();
            await environmentSessionDataHandler.ProcessAsync(environmentSessionData, handlerContext, vmResourceId, logger);
            Assert.Equal(handlerContext.CloudEnvironment.LastUsed, environmentSessionData.Timestamp);
        }

        [Fact]
        public async void DoNotUpdateLastUsedWithNoActiveUser()
        {
            var environmentSessionData = CreateEnvironmentSessionDataObject(connectionCount: 0);
            var handlerContext = await GetHandlerContext();
            await environmentSessionDataHandler.ProcessAsync(environmentSessionData, handlerContext, vmResourceId, logger);
            var cloudEnvironment = await GetCloudEnvironment();
            Assert.NotEqual(cloudEnvironment.LastUsed, environmentSessionData.Timestamp);
        }

        [Fact]
        public async void DoNotUpdateLastUsedWhenHeartBeatTimeStampIsOlder()
        {
            var handlerContext = await GetHandlerContext();
            var existingLastUpdate = handlerContext.CloudEnvironment.LastUsed;

            var environmentSessionData = CreateEnvironmentSessionDataObject();
            environmentSessionData.Timestamp = existingLastUpdate.AddSeconds(-30);

            await environmentSessionDataHandler.ProcessAsync(environmentSessionData, handlerContext, vmResourceId, logger);
            Assert.Equal(handlerContext.CloudEnvironment.LastUsed, existingLastUpdate);
        }

        [Fact]
        public async void IgnoreDataWhenEnvironmentIdIsMissing()
        {
            var environmentSessionData = CreateEnvironmentSessionDataObject(environmentId: null);
            var handlerContext = await GetHandlerContext();

            await environmentSessionDataHandler.ProcessAsync(environmentSessionData, handlerContext, vmResourceId, logger);
            Assert.NotEqual(handlerContext.CloudEnvironment.LastUsed, environmentSessionData.Timestamp);
        }


        [Fact]
        public async void ThrowValidationExceptonWhenCloudEnvironmentIsNotFound()
        {
            UpdateCloudEnvironmentForTest(null);
            var handlerContext = await GetHandlerContext();
            await Assert.ThrowsAsync<ValidationException>(() => environmentSessionDataHandler.ProcessAsync(CreateEnvironmentSessionDataObject(), handlerContext, vmResourceId, logger));
        }

        [Fact]
        public async void ThrowValidationExceptionWhenEnvironmentIsDeleted()
        {
            var cloudEnvironment = await GetCloudEnvironment();
            cloudEnvironment.State = CloudEnvironmentState.Deleted;
            UpdateCloudEnvironmentForTest(cloudEnvironment);
            var handlerContext = await GetHandlerContext();
            await Assert.ThrowsAsync<ValidationException>(() => environmentSessionDataHandler.ProcessAsync(CreateEnvironmentSessionDataObject(), handlerContext, vmResourceId, logger));
        }


        private EnvironmentSessionData CreateEnvironmentSessionDataObject(int connectionCount = 10, string environmentId = "1")
        {
            return new EnvironmentSessionData() { ConnectedSessionCount = connectionCount, Timestamp = DateTime.UtcNow, EnvironmentId = environmentId };
        }



    }
}
