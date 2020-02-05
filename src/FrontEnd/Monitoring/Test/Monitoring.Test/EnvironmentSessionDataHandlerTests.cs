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
             await Assert.ThrowsAsync<InvalidOperationException>(() => environmentSessionDataHandler.ProcessAsync(testEnvironmentData, vmResourceId, logger));
        }

        [Fact]
        public async void UpdateLastUsedWithActiveUsers()
        {
            var environmentSessionData = CreateEnvironmentSessionDataObject();
            await environmentSessionDataHandler.ProcessAsync(environmentSessionData, vmResourceId, logger);
            var cloudEnvironment = await GetCloudEnvironment();
            Assert.Equal(cloudEnvironment.LastUsed, environmentSessionData.Timestamp);
        }

        [Fact]
        public async void DoNotUpdateLastUsedWithNoActiveUser()
        {
            var environmentSessionData = CreateEnvironmentSessionDataObject(connectionCount: 0);
            await environmentSessionDataHandler.ProcessAsync(environmentSessionData, vmResourceId, logger);
            var cloudEnvironment = await GetCloudEnvironment();
            Assert.NotEqual(cloudEnvironment.LastUsed, environmentSessionData.Timestamp);
        }

        [Fact]
        public async void DoNotUpdateLastUsedWhenHeartBeatTimeStampIsOlder()
        {
            var existingCloudEnvironment = await GetCloudEnvironment();
            var existingLastUpdate = existingCloudEnvironment.LastUsed;

            var environmentSessionData = CreateEnvironmentSessionDataObject();
            environmentSessionData.Timestamp = existingLastUpdate.AddSeconds(-30);

            await environmentSessionDataHandler.ProcessAsync(environmentSessionData, vmResourceId, logger);
            Assert.Equal(existingCloudEnvironment.LastUsed, existingLastUpdate);
        }

        [Fact]
        public async void UpdateLastUpdatedHeartBeat()
        {
            var environmentSessionData = CreateEnvironmentSessionDataObject();
            await environmentSessionDataHandler.ProcessAsync(environmentSessionData, vmResourceId, logger);
            var cloudEnvironment = await GetCloudEnvironment();
            Assert.Equal(cloudEnvironment.LastUpdatedByHeartBeat, environmentSessionData.Timestamp);
        }

        [Fact]
        public async void IgnoreDataWhenEnvironmentIdIsMissing()
        {
            var environmentSessionData = CreateEnvironmentSessionDataObject(environmentId: null);
            await environmentSessionDataHandler.ProcessAsync(environmentSessionData, vmResourceId, logger);
            var cloudEnvironment = await GetCloudEnvironment();
            Assert.NotEqual(cloudEnvironment.LastUsed, environmentSessionData.Timestamp);
            Assert.NotEqual(cloudEnvironment.LastUpdatedByHeartBeat, environmentSessionData.Timestamp);
        }

        [Fact]
        public async void ThrowValidationExceptonWhenCloudEnvironmentIsNotFound()
        {
            UpdateCloudEnvironmentForTest(null);
            await Assert.ThrowsAsync<ValidationException>(() => environmentSessionDataHandler.ProcessAsync(CreateEnvironmentSessionDataObject(), vmResourceId, logger));
        }

        [Fact]
        public async void ThrowValidationExceptionWhenEnvironmentIsDeleted()
        {
            var cloudEnvironment = await GetCloudEnvironment();
            cloudEnvironment.State = CloudEnvironmentState.Deleted;
            UpdateCloudEnvironmentForTest(cloudEnvironment);
            await Assert.ThrowsAsync<ValidationException>(() => environmentSessionDataHandler.ProcessAsync(CreateEnvironmentSessionDataObject(), vmResourceId, logger));
        }


        private EnvironmentSessionData CreateEnvironmentSessionDataObject(int connectionCount = 10, string environmentId = "1")
        {
            return new EnvironmentSessionData() { ConnectedSessionCount = connectionCount, Timestamp = DateTime.UtcNow, EnvironmentId = environmentId };
        }


   
    }
}
