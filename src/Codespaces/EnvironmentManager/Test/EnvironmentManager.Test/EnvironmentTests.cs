using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Moq;
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
            var ex = await Assert.ThrowsAsync<ConflictException>(async () => await CreateTestEnvironmentAsync("abc"));
            Assert.Equal((int)MessageCodes.EnvironmentNameAlreadyExists, ex.MessageCode);
            Assert.Equal(environmentToDelete.FriendlyName.ToLowerInvariant(), environmentToDelete.FriendlyNameInLowerCase);

            // Deleting environment after environment name validation.
            var deleteResult = await this.environmentManager.DeleteAsync(environmentToDelete, logger);
            Assert.True(deleteResult);
        }

        [Fact]
        public async Task EnvironmentServiceUri()
        {
            var result = await CreateTestEnvironmentAsync("ABC");

            Assert.NotNull(result.Connection);
            Assert.Equal(testServiceUri.AbsoluteUri, result.Connection.ConnectionServiceUri);

            var environment2 = await this.environmentManager.GetAsync(Guid.Parse(result.Id), logger);
            Assert.NotNull(environment2.Connection);
            Assert.Equal(testServiceUri.AbsoluteUri, result.Connection.ConnectionServiceUri);
        }

        [Theory]
        [InlineData(Common.Continuation.OperationState.InProgress, false)]
        [InlineData(Common.Continuation.OperationState.Succeeded, true)]
        [InlineData(Common.Continuation.OperationState.Failed, true)]
        [InlineData(Common.Continuation.OperationState.Cancelled, true)]
        public async Task ShutdownContinuation_NoExtraContinuations(Common.Continuation.OperationState operationState, bool called)
        {
            var result = await CreateTestEnvironmentAsync("ABC");
            result.OSDisk = new ResourceAllocation.ResourceAllocationRecord()
            {
                ResourceId = Guid.NewGuid(),
            };

            result.Transitions.ShuttingDown = new TransitionState()
            {
                Status = operationState,
            };

            await this.environmentManager.SuspendCallbackAsync(result, this.logger);
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
