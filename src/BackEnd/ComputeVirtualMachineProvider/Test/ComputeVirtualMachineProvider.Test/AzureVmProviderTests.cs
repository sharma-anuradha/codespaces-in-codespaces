using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Test
{
    public class AzureVmProviderTests : IClassFixture<AzureVmProviderTestsBase>
    {
        private const int TargetVmCount = 1;
        private AzureVmProviderTestsBase testContext;
        public AzureVmProviderTests(AzureVmProviderTestsBase data)
        {
            this.testContext = data;
        }

        [Fact(Skip = "integration test")]
        //[Fact]
        public async Task VirtualMachine_Create_Start_Ok()
        {
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
            VirtualMachineProviderCreateResult createResult = await computeProvider.CreateAsync(input, null);
            timerCreate.Stop();
            Console.WriteLine($"Time taken to begin create VM {timerCreate.Elapsed.TotalSeconds}");
            var timerWait = Stopwatch.StartNew();
            Assert.NotNull(createResult);
            Assert.Equal("InProgress", createResult.Status);
            DeploymentStatusInput createDeploymentStatusInput = createResult.ContinuationToken.ToDeploymentStatusInput();
            Assert.NotNull(createDeploymentStatusInput);
            Assert.NotNull(createDeploymentStatusInput.TrackingId);
            Assert.Equal(rgName, createDeploymentStatusInput.ResourceId.ResourceGroup);
            Assert.NotEqual(Guid.Empty, createDeploymentStatusInput.ResourceId.InstanceId);
            Assert.Equal(subscriptionId, createDeploymentStatusInput.ResourceId.SubscriptionId);
            Assert.Equal(location, createDeploymentStatusInput.ResourceId.Location);


            VirtualMachineProviderCreateResult statusCheckResult = default;
            do
            {
                await Task.Delay(500);
                statusCheckResult = await computeProvider.CreateAsync(input, createResult.ContinuationToken);
                Assert.NotNull(statusCheckResult);
                Assert.True(statusCheckResult.Status.Equals(DeploymentState.InProgress.ToString()) || statusCheckResult.Status.Equals(DeploymentState.Succeeded.ToString()));
                if (statusCheckResult.Status.Equals(DeploymentState.InProgress.ToString()))
                {
                    DeploymentStatusInput statusCheckdeploymentStatusToken = statusCheckResult.ContinuationToken.ToDeploymentStatusInput();
                    Assert.NotNull(statusCheckdeploymentStatusToken);
                }
            } while (statusCheckResult.Status.Equals(DeploymentState.InProgress.ToString()));
            timerWait.Stop();
            Console.WriteLine($"Time taken to create VM {timerWait.Elapsed.TotalSeconds}");
        }

        [Fact(Skip = "integration test")]
        //[Fact]
        public async Task VirtualMachine_Create_Multiple_VM_Ok()
        {
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
            VirtualMachineProviderCreateResult[] initiateVmCreationList = await Task.WhenAll(Enumerable.Range(0, TargetVmCount).Select(x => computeProvider.CreateAsync(input, null)));
            timerCreate.Stop();
            Console.WriteLine($"Time taken to begin create 10 VMs {timerCreate.Elapsed.TotalSeconds}");

            var timerWait = Stopwatch.StartNew();
            VirtualMachineProviderCreateResult[] vmStatus = await Task.WhenAll(initiateVmCreationList.Select(x => WaitForVMCreation(computeProvider, input, x)));
            timerWait.Stop();
            System.Console.WriteLine($"Time taken to create VM {timerWait.Elapsed.TotalSeconds}");
        }

        [Fact(Skip = "integration test")]
        public async Task Start_Compute_Ok()
        {
            var azureDeploymentManager = new LinuxVirtualMachineManager(new AzureClientFactoryMock(testContext.AuthFilePath));
            var computeProvider = new VirtualMachineProvider(azureDeploymentManager);
            var fileShareInfo = new ShareConnectionInfo("storageaccount1",
                                                       "accountkey",
                                                       "cloudenvdata",
                                                       "dockerlib");
            ResourceId resourceId = new ResourceId(
                ResourceType.ComputeVM,
                Guid.Parse("47b6d3d7-26f3-4fed-9aa8-fa809b0dd3cc"),
                testContext.SubscriptionId,
                "vsclk-core-dev-test",
                AzureLocation.WestUs2);
            var startComputeInput = new VirtualMachineProviderStartComputeInput(
                resourceId,
                fileShareInfo,
                new Dictionary<string, string>() {
                    { "SESSION_ID", "value1" },
                    { "SESSION_TOKEN", "value2" },
                    { "SESSION_CALLBACK", "value2" } });

            await StartCompute(computeProvider, startComputeInput);
        }

        [Fact(Skip = "integration test")]
        //[Fact]
        public async Task Delete_Compute_Ok()
        {
            var deleteTimer = Stopwatch.StartNew();
            var azureDeploymentManager = new LinuxVirtualMachineManager(new AzureClientFactoryMock(testContext.AuthFilePath));
            var computeProvider = new VirtualMachineProvider(azureDeploymentManager);
            VirtualMachineProviderDeleteInput input = new VirtualMachineProviderDeleteInput
            {
                ResourceId = new ResourceId(ResourceType.ComputeVM,
                  Guid.Parse("894adc3d-758c-41ec-bb58-d7cf0640a676"),
                  testContext.SubscriptionId,
                  testContext.ResourceGroupName,
                  testContext.Location),
            };

            var deleteResult = await computeProvider.DeleteAsync(input);
            deleteTimer.Stop();
            System.Console.WriteLine($"Time taken to create VM {deleteTimer.Elapsed.TotalSeconds}");

            VirtualMachineProviderDeleteResult deleteStatusCheckResult = deleteResult;
            var timerWait = Stopwatch.StartNew();
            do
            {
                await Task.Delay(500);
                deleteStatusCheckResult = await computeProvider.DeleteAsync(input, deleteStatusCheckResult.ContinuationToken);
                Assert.NotNull(deleteStatusCheckResult);
                Assert.True(deleteStatusCheckResult.Status.Equals(DeploymentState.InProgress.ToString())
                    || deleteStatusCheckResult.Status.Equals(DeploymentState.Succeeded.ToString()));
                if (deleteStatusCheckResult.Status.Equals(DeploymentState.InProgress.ToString()))
                {
                    DeploymentStatusInput statusCheckdeploymentStatusToken = deleteStatusCheckResult.ContinuationToken.ToDeploymentStatusInput();
                    Assert.NotNull(statusCheckdeploymentStatusToken);
                }
            } while (deleteStatusCheckResult.Status.Equals(DeploymentState.InProgress.ToString()));
            timerWait.Stop();
            System.Console.WriteLine($"Time taken to start environment on VM {timerWait.Elapsed.TotalSeconds}");
        }

        private static async Task<VirtualMachineProviderCreateResult> WaitForVMCreation(VirtualMachineProvider computeProvider, VirtualMachineProviderCreateInput input, VirtualMachineProviderCreateResult vmToken)
        {
            VirtualMachineProviderCreateResult statusCheckResult = default;
            do
            {
                await Task.Delay(500);
                statusCheckResult = await computeProvider.CreateAsync(input, vmToken.ContinuationToken);
                Assert.NotNull(statusCheckResult);
                Assert.True(statusCheckResult.Status.Equals(DeploymentState.InProgress.ToString()) || statusCheckResult.Status.Equals(DeploymentState.Succeeded.ToString()));
                if (statusCheckResult.Status.Equals(DeploymentState.InProgress.ToString()))
                {
                    DeploymentStatusInput statusCheckdeploymentStatusToken = statusCheckResult.ContinuationToken.ToDeploymentStatusInput();
                    Assert.NotNull(statusCheckdeploymentStatusToken);
                }
            } while (statusCheckResult.Status.Equals(DeploymentState.InProgress.ToString()));
            return statusCheckResult;
        }

        private static async Task StartCompute(VirtualMachineProvider computeProvider, VirtualMachineProviderStartComputeInput startComputeInput)
        {
            var timerStartCompute = Stopwatch.StartNew();
            VirtualMachineProviderStartComputeResult startComputeResult = await computeProvider.StartComputeAsync(startComputeInput);
            timerStartCompute.Stop();
            System.Console.WriteLine($"Time taken to allocate VM {timerStartCompute.Elapsed.TotalSeconds}");
            Assert.NotNull(startComputeResult);
            Assert.Equal("InProgress", startComputeResult.Status);
            DeploymentStatusInput startComputeStatusCheckInput = startComputeResult.ContinuationToken.ToDeploymentStatusInput();
            Assert.NotNull(startComputeStatusCheckInput);
            Assert.NotNull(startComputeStatusCheckInput.TrackingId);
            Assert.Equal(startComputeInput.ResourceId.ResourceGroup, startComputeStatusCheckInput.ResourceId.ResourceGroup);
            Assert.NotEqual(Guid.Empty, startComputeStatusCheckInput.ResourceId.InstanceId);
            Assert.Equal(startComputeInput.ResourceId.SubscriptionId, startComputeStatusCheckInput.ResourceId.SubscriptionId);
            Assert.Equal(startComputeInput.ResourceId.Location, startComputeStatusCheckInput.ResourceId.Location);


            VirtualMachineProviderStartComputeResult statusCheckResult = default;
            var timerWait = Stopwatch.StartNew();
            do
            {
                await Task.Delay(500);
                statusCheckResult = await computeProvider.StartComputeAsync(startComputeInput, startComputeResult.ContinuationToken);
                Assert.NotNull(statusCheckResult);
                Assert.True(statusCheckResult.Status.Equals(DeploymentState.InProgress.ToString()) || statusCheckResult.Status.Equals(DeploymentState.Succeeded.ToString()));
                if (statusCheckResult.Status.Equals(DeploymentState.InProgress.ToString()))
                {
                    DeploymentStatusInput statusCheckdeploymentStatusToken = statusCheckResult.ContinuationToken.ToDeploymentStatusInput();
                    Assert.NotNull(statusCheckdeploymentStatusToken);
                }
            } while (statusCheckResult.Status.Equals(DeploymentState.InProgress.ToString()));
            timerWait.Stop();
            System.Console.WriteLine($"Time taken to start environment on VM {timerWait.Elapsed.TotalSeconds}");
        }
    }
}