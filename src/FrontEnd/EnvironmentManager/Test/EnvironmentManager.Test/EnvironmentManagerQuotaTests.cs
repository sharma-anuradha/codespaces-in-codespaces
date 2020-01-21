using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
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
            var environmentToDelete = await CreateTestEnvironmentAsync("Test-1");

            for (var i = 2; i<=20; i++)
            {
                await CreateTestEnvironmentAsync($"Test-{i}");
            }

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
            var canSaveResult = await CreateTestEnvironmentAsync($"Test-1");
            Assert.Equal(StatusCodes.Status200OK, canSaveResult.HttpStatusCode);
            Assert.NotNull(canSaveResult.CloudEnvironment);
        }
    }
}
