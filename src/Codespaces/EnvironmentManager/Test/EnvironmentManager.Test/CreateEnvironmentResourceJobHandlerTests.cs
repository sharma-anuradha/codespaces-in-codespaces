using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Test
{
    public class CreateEnvironmentResourceJobHandlerTests : EnvironmentManagerTestsBase
    {
        private const AzureLocation testLocation = AzureLocation.WestUs2;
        private const string skuName = "standardLinux";
        private EnvironmentPool testEnvironmentPool;

        public CreateEnvironmentResourceJobHandlerTests()
        {
            var details = new EnvironmentPoolDetails() { Location = testLocation, SkuName = skuName, };

            testEnvironmentPool = new EnvironmentPool() { Id = Guid.NewGuid().ToString(), IsEnabled = true, TargetCount = 5, Details = details, };
        }

        [Fact]
        public async Task CreateEnvironmentResourceJobHandler_AllStages_Succeeds()
        {
            var queueFactory = new BufferBlockQueueFactory();
            var jobQueueProducerFactory = new JobQueueProducerFactory(queueFactory);

            var jobHandler = new CreateEnvironmentResourceJobHandler(
                environmentRepository,
                heartbeatRepository,
                resourceBroker,
                resourceAllocationManager,
                MockUtil.MockResourceSelectorFactory(),
                jobQueueProducerFactory);

            var envId = Guid.NewGuid();

            var jobInput = new CreateEnvironmentResourceJobHandler.Payload()
            {
                EntityId = envId,
                Pool = testEnvironmentPool,
                Reason = "WatchPoolSizeIncrease",
                LoggerProperties = new Dictionary<string, object>(),
                CurrentState = CreateEnvironmentResourceJobHandler.JobState.AllocateResource,
            };

            var mockJob = new Mock<IJob<CreateEnvironmentResourceJobHandler.Payload>>();
            mockJob.SetupGet(x => x.Payload).Returns(() => jobInput);
            mockJob.SetupGet(x => x.Queue).Returns(() => queueFactory.GetOrCreate(CreateEnvironmentResourceJobHandler.DefaultQueueId, testLocation));

            // Payload initialized
            await jobHandler.HandleJobAsync(mockJob.Object, logger, CancellationToken.None);

            // Actually do some work
            await jobHandler.HandleJobAsync(mockJob.Object, logger, CancellationToken.None);

            Assert.Equal(CreateEnvironmentResourceJobHandler.JobState.CheckResourceState, jobInput.CurrentState);
            var validations = new List<Action<CloudEnvironment>>()
            {
                (codespace) =>
                {
                    Assert.NotNull(codespace);
                    Assert.Equal(CloudEnvironmentState.Created, codespace.State);
                    Assert.Equal(skuName, codespace.SkuName);
                    Assert.Equal(testLocation, codespace.Location);
                    Assert.NotNull(codespace.PoolReference);
                    Assert.Equal(testEnvironmentPool.Details.GetPoolDefinition(), codespace.PoolReference.Code);
                    Assert.False(codespace.IsAssigned);
                    Assert.Null(codespace.Assigned);
                    Assert.NotNull(codespace.Compute);
                    Assert.NotNull(codespace.Storage);
                    Assert.False(codespace.IsReady);
                    Assert.Null(codespace.Ready);
                }
            };

            await ValidateCodespace(envId, validations);

            await jobHandler.HandleJobAsync(mockJob.Object, logger, CancellationToken.None);

            Assert.Equal(CreateEnvironmentResourceJobHandler.JobState.StartHeartbeatMonitoring, jobInput.CurrentState);

            validations.Add((codespace) =>
            {
                Assert.True(codespace.Storage.IsReady);
                Assert.True(codespace.Compute.IsReady);
            });

            await ValidateCodespace(envId, validations);

            await jobHandler.HandleJobAsync(mockJob.Object, logger, CancellationToken.None);

            validations = new List<Action<CloudEnvironment>>()
            {
                (codespace) =>
                {
                    Assert.NotNull(codespace);
                    Assert.Equal(skuName, codespace.SkuName);
                    Assert.Equal(testLocation, codespace.Location);
                    Assert.Equal(CloudEnvironmentState.Created, codespace.State);
                    Assert.False(codespace.IsAssigned);
                    Assert.Null(codespace.Assigned);
                    Assert.NotNull(codespace.Compute);
                    Assert.NotNull(codespace.Storage);
                    Assert.NotNull(codespace.HeartbeatResourceId);
                    Assert.True(codespace.IsReady);
                    Assert.NotNull(codespace.Ready);
                }
            };

            await ValidateCodespace(envId, validations);
        }

        private async Task ValidateCodespace(Guid envId, List<Action<CloudEnvironment>> validations)
        {
            var codespace = await environmentRepository.GetAsync(envId.ToString(), logger);
            foreach (var validation in validations)
            {
                validation(codespace);
            }
        }
    }
}
