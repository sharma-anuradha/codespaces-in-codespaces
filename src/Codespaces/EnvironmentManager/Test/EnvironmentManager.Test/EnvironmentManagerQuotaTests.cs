using AutoMapper.Configuration.Conventions;
using Microsoft.AspNetCore.Http;
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
                logger, planId: testPlan.ResourceId);

            Assert.Equal(20, listEnvironments.Count());

            // 21st envrionment should not be created
            var result = await CreateTestEnvironmentAsync("Test21");
            Assert.Equal(MessageCodes.ExceededQuota, result.MessageCode);
            Assert.Equal(StatusCodes.Status403Forbidden, result.HttpStatusCode);

            // Delete 1 environment.
            var deleteResult = await this.environmentManager.DeleteAsync(environmentToDelete.CloudEnvironment, logger);
            Assert.True(deleteResult);

            // User should be allowed to create environment.
            var canSaveResult = await CreateTestEnvironmentAsync($"Test-0");
            Assert.Equal(StatusCodes.Status200OK, canSaveResult.HttpStatusCode);
            Assert.NotNull(canSaveResult.CloudEnvironment);
        }

        [Fact]
        public async Task WindowsEnvironmentCreationChecksFlag()
        {
            // Max Compute Core Quota = 10
            // Compute Core per SKU = 1
            await CreateEnvironmentsAsync(10, "Round1", "windows");

            var listEnvironments = await this.environmentManager.ListAsync(
               logger, planId: testPlan.ResourceId);

            // Subscription is at Max Compute Cores
            Assert.Equal(10, listEnvironments.Count());

            // Create 10 more cores
            await CreateEnvironmentsAsync(10, "Round2", "windows");

            listEnvironments = await this.environmentManager.ListAsync(
               logger, planId: testPlan.ResourceId);

            // Subscription is allowed to go over Default Max Copute cores
            Assert.Equal(20, listEnvironments.Count());
        }

        [Fact]
        public async Task WindowsEnvironmentResumeChecksFlag()
        {
            // Default Max Compute Quota = 10
            // Compute Core per SKU = 1
            var result = await CreateTestEnvironmentAsync("Test0");
            var environmentToResume = result.CloudEnvironment;
            await this.environmentManager.ForceSuspendAsync(environmentToResume, logger);

            await CreateEnvironmentsAsync(10, "windows");

            var listEnvironments = await this.environmentManager.ListAsync(
               logger, planId: testPlan.ResourceId);

            // Subscription is over Max Compute Cores
            Assert.Equal(11, listEnvironments.Count());

            var startEnvironmentParams = new StartCloudEnvironmentParameters
            {
                UserProfile = MockProfile(),
                ConnectionServiceUri = new Uri("http://localhost/"),
                CallbackUriFormat = "http://localhost/{0}",
                FrontEndServiceUri = new Uri("http://localhost/"),
            };

            await this.environmentManager.ResumeAsync(environmentToResume, startEnvironmentParams, Subscription, logger);

            // Subscription is allowed to Resume cores that go over Default Max Compute Cores
            Assert.Equal(11, listEnvironments.Count());
        }

        private async Task CreateEnvironmentsAsync(
            int numOfEnvironments,
            string namePrefix = "Test",
            string skuName = "test")
        {
            for (var i = 1; i <= numOfEnvironments; i++)
            {
                await CreateTestEnvironmentAsync($"{namePrefix}-{i}", skuName);
            }
        }
    }
}
