using AutoMapper.Configuration.Conventions;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.Azure.Management.Cdn.Fluent.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Test
{
    public class EnvironmentManagerQuotaTests : EnvironmentManagerTestsBase
    {
        [Fact]
        public async Task EnvironmentCreationChecksQuota()
        {
            var environmentToDelete = await CreateTestEnvironmentAsync("Test-0");

            await CreateEnvironmentsAsync(19);

            // 20 environments exist
            var listEnvironments = await this.environmentManager.ListAsync(
                testPlan.ResourceId, testPlan.Location, null, null, EnvironmentListType.ActiveEnvironments, logger);

            Assert.Equal(20, listEnvironments.Count());

            // 21st envrionment should not be created
            var ex = await Assert.ThrowsAsync<ForbiddenException>(async () => await CreateTestEnvironmentAsync("Test21"));
            Assert.Equal((int)MessageCodes.ExceededQuota, ex.MessageCode);

            // Soft Delete 1 environment.
            var deleteResult = await this.environmentManager.SoftDeleteAsync(Guid.Parse(environmentToDelete.Id), logger);
            Assert.True(deleteResult);

            // User should be allowed to create environment.
            var canSaveResult = await CreateTestEnvironmentAsync($"Test-0");
            Assert.NotNull(canSaveResult);
        }

        [Fact]
        public async Task WindowsEnvironmentCreationChecksFlag()
        {
            // Max Compute Core Quota = 10
            // Compute Core per SKU = 1
            await CreateEnvironmentsAsync(10, "Round1", "windows");

            var listEnvironments = await this.environmentManager.ListAsync(testPlan.ResourceId, testPlan.Location, null, null, EnvironmentListType.ActiveEnvironments, logger);

            // Subscription is at Max Compute Cores
            Assert.Equal(10, listEnvironments.Count());

            // Create 10 more cores
            await CreateEnvironmentsAsync(10, "Round2", "windows");

            listEnvironments = await this.environmentManager.ListAsync(testPlan.ResourceId, testPlan.Location, null, null, EnvironmentListType.ActiveEnvironments, logger);

            // Subscription is allowed to go over Default Max Compute cores
            Assert.Equal(20, listEnvironments.Count());
        }

        [Fact]
        public async Task WindowsEnvironmentResumeChecksFlag()
        {
            // Default Max Compute Quota = 10
            // Compute Core per SKU = 1
            var environmentToResume = await CreateTestEnvironmentAsync("Test0", "windows");
            await this.environmentManager.ForceSuspendAsync(Guid.Parse(environmentToResume.Id), logger);

            await CreateEnvironmentsAsync(10, "windows", "windows");

            var listEnvironments = await this.environmentManager.ListAsync(testPlan.ResourceId, testPlan.Location, null, null, EnvironmentListType.ActiveEnvironments, logger);

            // Subscription is over Max Compute Cores
            Assert.Equal(11, listEnvironments.Count());

            var startEnvironmentParams = new StartCloudEnvironmentParameters
            {
                UserProfile = MockUtil.MockProfile(),
                ConnectionServiceUri = new Uri("http://localhost/"),
                CallbackUriFormat = "http://localhost/{0}",
                FrontEndServiceUri = new Uri("http://localhost/"),
            };

            await this.environmentManager.ResumeAsync(Guid.Parse(environmentToResume.Id), startEnvironmentParams, logger);

            // Subscription is allowed to Resume cores that go over Default Max Compute Cores
            Assert.Equal(11, listEnvironments.Count());
        }

        private async Task CreateEnvironmentsAsync(
            int numOfEnvironments,
            string namePrefix = "Test",
            string skuName = "windows")
        {
            for (var i = 1; i <= numOfEnvironments; i++)
            {
                await CreateTestEnvironmentAsync($"{namePrefix}-{i}", skuName);
            }
        }
    }
}
