using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Diagnostics;
using Moq;
using System;
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

        [Theory]
        [InlineData(Common.Continuation.OperationState.InProgress, false)]
        [InlineData(Common.Continuation.OperationState.Succeeded, true)]
        [InlineData(Common.Continuation.OperationState.Failed, true)]
        [InlineData(Common.Continuation.OperationState.Cancelled, true)]
        public async Task ShutdownContinuation_NoExtraContinuations(Common.Continuation.OperationState operationState, bool called)
        {
            var result = await CreateTestEnvironmentAsync("ABC");
            result.CloudEnvironment.OSDisk = new ResourceAllocation.ResourceAllocationRecord()
            {
                ResourceId = Guid.NewGuid(),
            };

            result.CloudEnvironment.Transitions.ShuttingDown = new TransitionState()
            {
                Status = operationState,
            };

            await this.environmentManager.SuspendCallbackAsync(result.CloudEnvironment, this.logger);
            if (called)
            {
                environmentContinuationOperations.Verify(x => x.ShutdownAsync(It.IsAny<System.Guid>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()), Times.Once);
            }
            else
            {
                environmentContinuationOperations.Verify(x => x.ShutdownAsync(It.IsAny<System.Guid>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()), Times.Never);
            }
        }
    }
}
