using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
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
        private AzureVmProviderTestsBase testContext;
        public AzureVmProviderTests(AzureVmProviderTestsBase data)
        {
            this.testContext = data;
        }

        [Fact]
        public async Task VirtualMachine_Create_Initiate_Ok()
        {
            var azureDeploymentManager = new AzureDeploymentManager(new AzureClientFactoryMock(testContext.AuthFilePath));

            var computeProvider = new VirtualMachineProvider(azureDeploymentManager);
            Guid subscriptionId = this.testContext.SubscriptionId;
            const AzureLocation eastUs = AzureLocation.EastUs;
            string rgName = testContext.ResourceGroupName;

            VirtualMachineProviderCreateInput input = new VirtualMachineProviderCreateInput()
            {
                AzureVmLocation = eastUs,
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
            Assert.Equal(eastUs, createDeploymentStatusInput.ResourceId.Location);


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
            System.Console.WriteLine($"Time taken to create VM {timerWait.Elapsed.TotalSeconds}");
            var fileShareInfo = new ShareConnectionInfo("storageaccount1",
                                                       "accountkey",
                                                       "cloudenvdata",
                                                       "dockerlib");
            var startComputeInput = new VirtualMachineProviderStartComputeInput(createDeploymentStatusInput.ResourceId,
                                                                 fileShareInfo,
                                                                 new Dictionary<string, string>() {
                                                                     { "SESSION_ID", "value1" },
                                                                     { "SESSION_TOKEN", "value2" },
                                                                     { "SESSION_CALLBACK", "value2" } });

            await StartCompute(computeProvider, startComputeInput);
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