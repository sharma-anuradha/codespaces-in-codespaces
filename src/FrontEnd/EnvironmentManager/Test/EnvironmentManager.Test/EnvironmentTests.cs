using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Test
{
    public class EnvironmentTests : EnvironmentManagerTestsBase
    {
        [Fact]
        public async Task EnvironmentFriendlyNameCheck()
        {
            // User should be allowed to create environment.
            var environmentToDelete = await CreateTestEnvironmentAsync("ABC");

            // User should not be allowed to create environment.
            var result = await CreateTestEnvironmentAsync("abc");
            Assert.Equal(StatusCodes.Status409Conflict, result.HttpStatusCode);
            Assert.Equal(environmentToDelete.CloudEnvironment.FriendlyName.ToLowerInvariant(), environmentToDelete.CloudEnvironment.FriendlyNameInLowerCase);

            // Deleting environment after environment name validation.
            var deleteResult = await this.environmentManager.DeleteAsync(environmentToDelete.CloudEnvironment, logger);
            Assert.True(deleteResult);

        }

        [Fact]
        public async Task EnvironmentServiceUri()
        {
            var result = await CreateTestEnvironmentAsync("ABC");

            Assert.NotNull(result.CloudEnvironment.Connection);
            Assert.Equal(testServiceUri.AbsoluteUri, result.CloudEnvironment.Connection.ConnectionServiceUri);

            var environment2 = await this.environmentManager.GetAsync(result.CloudEnvironment.Id, logger);
            Assert.NotNull(environment2.Connection);
            Assert.Equal(testServiceUri.AbsoluteUri, result.CloudEnvironment.Connection.ConnectionServiceUri);
        }
    }
}
