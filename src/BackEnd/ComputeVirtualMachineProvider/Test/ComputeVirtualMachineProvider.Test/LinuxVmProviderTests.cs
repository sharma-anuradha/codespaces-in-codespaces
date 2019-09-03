using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Test
{
    public class LinuxVmProviderTests : IClassFixture<AzureVmProviderTestsBase>
    {
        private const int TargetVmCount = 1;
        private AzureVmProviderTestsBase testContext;
        public LinuxVmProviderTests(AzureVmProviderTestsBase data)
        {
            this.testContext = data;
        }

        [Trait("Category", "IntegrationTest")]
        [Fact]
        public async Task Create_Compute_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            var azureDeploymentManager = new LinuxVirtualMachineManager(new AzureClientFactoryMock(testContext.AuthFilePath));

            var computeProvider = new VirtualMachineProvider(azureDeploymentManager);
            Guid subscriptionId = this.testContext.SubscriptionId;
            AzureLocation location = testContext.Location;
            string rgName = testContext.ResourceGroupName;

            VirtualMachineProviderCreateInput input = new VirtualMachineProviderCreateInput()
            {
                AzureVmLocation = location,
                AzureResourceGroup = rgName,
                AzureSubscription = subscriptionId,
                AzureVirtualMachineImage = "Canonical.UbuntuServer.18.04-LTS.latest",
                AzureSkuName = "Standard_F4s_v2",
            };

            var timerCreate = Stopwatch.StartNew();
            VirtualMachineProviderCreateResult createResult = await computeProvider.CreateAsync(input, logger);
            timerCreate.Stop();
            Console.WriteLine($"Time taken to begin create VM {timerCreate.Elapsed.TotalSeconds}");
            var timerWait = Stopwatch.StartNew();
            Assert.NotNull(createResult);
            Assert.Equal(OperationState.InProgress, createResult.Status);
            NextStageInput createDeploymentStatusInput = createResult.NextInput.ContinuationToken.ToNextStageInput();
            Assert.NotNull(createDeploymentStatusInput);
            Assert.NotNull(createDeploymentStatusInput.TrackingId);
            Assert.Equal(rgName, createDeploymentStatusInput.AzureResourceInfo.ResourceGroup);
            Assert.Equal(subscriptionId, createDeploymentStatusInput.AzureResourceInfo.SubscriptionId);

            VirtualMachineProviderCreateResult statusCheckResult = default;
            do
            {
                await Task.Delay(500);
                statusCheckResult = await computeProvider.CreateAsync(input, logger);
                Assert.NotNull(statusCheckResult);
                Assert.True(statusCheckResult.Status.Equals(OperationState.InProgress) || statusCheckResult.Status.Equals(OperationState.Succeeded));
                if (statusCheckResult.Status.Equals(OperationState.InProgress))
                {
                    NextStageInput statusCheckdeploymentStatusToken = statusCheckResult.NextInput.ContinuationToken.ToNextStageInput();
                    Assert.NotNull(statusCheckdeploymentStatusToken);
                }
            } while (statusCheckResult.Status.Equals(OperationState.InProgress));
            timerWait.Stop();
            Console.WriteLine($"Time taken to create VM {timerWait.Elapsed.TotalSeconds}");
        }

        [Trait("Category", "IntegrationTest")]
        [Fact]
        public async Task Create_Multiple_Compute_Ok()
        {
            var logger = new DefaultLoggerFactory().New();           
            var azureDeploymentManager = new LinuxVirtualMachineManager(new AzureClientFactoryMock(testContext.AuthFilePath));
            var computeProvider = new VirtualMachineProvider(azureDeploymentManager);
            Guid subscriptionId = this.testContext.SubscriptionId;
            AzureLocation location = testContext.Location;
            string rgName = testContext.ResourceGroupName;

            VirtualMachineProviderCreateInput input = new VirtualMachineProviderCreateInput()
            {
                AzureVmLocation = location,
                AzureResourceGroup = rgName,
                AzureSubscription = subscriptionId,
                AzureVirtualMachineImage = "Canonical.UbuntuServer.18.04-LTS.latest",
                AzureSkuName = "Standard_F4s_v2",
            };

            var timerCreate = Stopwatch.StartNew();
            VirtualMachineProviderCreateResult[] initiateVmCreationList = await Task.WhenAll(Enumerable.Range(0, TargetVmCount).Select(x => computeProvider.CreateAsync(input, logger)));
            timerCreate.Stop();
            Console.WriteLine($"Time taken to begin create 10 VMs {timerCreate.Elapsed.TotalSeconds}");

            var timerWait = Stopwatch.StartNew();
            VirtualMachineProviderCreateResult[] vmStatus = await Task.WhenAll(initiateVmCreationList.Select(x => WaitForVMCreation(computeProvider, input, logger)));
            timerWait.Stop();
            Console.WriteLine($"Time taken to create VM {timerWait.Elapsed.TotalSeconds}");
        }

        [Trait("Category", "IntegrationTest")]
        [Fact]
        public async Task Start_Compute_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            var azureDeploymentManager = new LinuxVirtualMachineManager(new AzureClientFactoryMock(testContext.AuthFilePath));
            var computeProvider = new VirtualMachineProvider(azureDeploymentManager);
            var fileShareInfo = new ShareConnectionInfo("storageaccount1",
                                                       "accountkey",
                                                       "cloudenvdata",
                                                       "dockerlib");
            var resourceId = Guid.Parse("47b6d3d7-26f3-4fed-9aa8-fa809b0dd3cc");
            var azureResourceInfo = new AzureResourceInfo(testContext.SubscriptionId, testContext.ResourceGroupName, resourceId.ToString());
            var startComputeInput = new VirtualMachineProviderStartComputeInput(
                azureResourceInfo,
                fileShareInfo,
                new Dictionary<string, string>()
                {
                    { "SESSION_ID", "value1" },
                    { "SESSION_TOKEN", "value2" },
                    { "SESSION_CALLBACK", "value2" }
                },
                null);

            await StartCompute(computeProvider, startComputeInput, logger);
        }

        [Trait("Category", "IntegrationTest")]
        [Fact]
        public async Task Delete_Compute_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            var deleteTimer = Stopwatch.StartNew();
            var azureDeploymentManager = new LinuxVirtualMachineManager(new AzureClientFactoryMock(testContext.AuthFilePath));
            var computeProvider = new VirtualMachineProvider(azureDeploymentManager);

            var resourceName = Guid.Parse("8a1a3a88-e79f-4f8e-ac49-e7cad3d45376").ToString();
            var input = new VirtualMachineProviderDeleteInput
            {
                AzureResourceInfo = new AzureResourceInfo(testContext.SubscriptionId, testContext.ResourceGroupName, resourceName),
            };

            var deleteResult = await computeProvider.DeleteAsync(input, logger);
            deleteTimer.Stop();
            Console.WriteLine($"Time taken to create VM {deleteTimer.Elapsed.TotalSeconds}");

            var deleteStatusCheckResult = deleteResult;
            var timerWait = Stopwatch.StartNew();
            do
            {
                await Task.Delay(500);
                deleteStatusCheckResult = await computeProvider.DeleteAsync((VirtualMachineProviderDeleteInput)deleteStatusCheckResult.NextInput, logger);
                Assert.NotNull(deleteStatusCheckResult);
                Assert.True(deleteStatusCheckResult.Status.Equals(OperationState.InProgress)
                    || deleteStatusCheckResult.Status.Equals(OperationState.Succeeded));
                if (deleteStatusCheckResult.Status.Equals(OperationState.InProgress))
                {
                    NextStageInput statusCheckdeploymentStatusToken = deleteStatusCheckResult.NextInput.ContinuationToken.ToNextStageInput();
                    Assert.NotNull(statusCheckdeploymentStatusToken);
                }
            } while (deleteStatusCheckResult.Status.Equals(OperationState.InProgress));
            timerWait.Stop();
            Console.WriteLine($"Time taken to start environment on VM {timerWait.Elapsed.TotalSeconds}");
        }

        private static async Task<VirtualMachineProviderCreateResult> WaitForVMCreation(VirtualMachineProvider computeProvider, VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger)
        {
            VirtualMachineProviderCreateResult statusCheckResult = default;
            do
            {
                await Task.Delay(500);
                statusCheckResult = await computeProvider.CreateAsync(input, logger);
                Assert.NotNull(statusCheckResult);
                Assert.True(statusCheckResult.Status.Equals(OperationState.InProgress) || statusCheckResult.Status.Equals(OperationState.Succeeded));
                if (statusCheckResult.Status.Equals(OperationState.InProgress))
                {
                    NextStageInput statusCheckdeploymentStatusToken = statusCheckResult.NextInput.ContinuationToken.ToNextStageInput();
                    Assert.NotNull(statusCheckdeploymentStatusToken);
                }
            } while (statusCheckResult.Status.Equals(OperationState.InProgress));
            return statusCheckResult;
        }

        private static async Task StartCompute(VirtualMachineProvider computeProvider, VirtualMachineProviderStartComputeInput startComputeInput, IDiagnosticsLogger logger)
        {
            var timerStartCompute = Stopwatch.StartNew();
            VirtualMachineProviderStartComputeResult startComputeResult = await computeProvider.StartComputeAsync(startComputeInput, logger);
            timerStartCompute.Stop();
            Console.WriteLine($"Time taken to allocate VM {timerStartCompute.Elapsed.TotalSeconds}");
            Assert.NotNull(startComputeResult);
            Assert.Equal(OperationState.InProgress, startComputeResult.Status);
            NextStageInput startComputeStatusCheckInput = startComputeResult.NextInput.ContinuationToken.ToNextStageInput();
            Assert.NotNull(startComputeStatusCheckInput);
            Assert.NotNull(startComputeStatusCheckInput.TrackingId);
            Assert.NotNull(startComputeStatusCheckInput.AzureResourceInfo);

            VirtualMachineProviderStartComputeResult statusCheckResult = default;
            var timerWait = Stopwatch.StartNew();
            do
            {
                await Task.Delay(500);
                statusCheckResult = await computeProvider.StartComputeAsync(startComputeInput, logger);
                Assert.NotNull(statusCheckResult);
                Assert.True(statusCheckResult.Status.Equals(OperationState.InProgress) || statusCheckResult.Status.Equals(OperationState.Succeeded));
                if (statusCheckResult.Status.Equals(OperationState.InProgress))
                {
                    NextStageInput statusCheckdeploymentStatusToken = statusCheckResult.NextInput.ContinuationToken.ToNextStageInput();
                    Assert.NotNull(statusCheckdeploymentStatusToken);
                }
            } while (statusCheckResult.Status.Equals(OperationState.InProgress));
            timerWait.Stop();
            Console.WriteLine($"Time taken to start environment on VM {timerWait.Elapsed.TotalSeconds}");
        }
    }
}