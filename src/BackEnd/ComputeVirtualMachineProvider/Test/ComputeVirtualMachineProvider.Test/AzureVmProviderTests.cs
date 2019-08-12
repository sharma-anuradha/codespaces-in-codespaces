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
            System.Console.WriteLine($"Time taken to begin create VM {timerCreate.Elapsed.TotalSeconds}");
            var timerWait = Stopwatch.StartNew();
            Assert.NotNull(createResult);
            Assert.Equal("InProgress", createResult.Status);
            DeploymentStatusInput createDeploymentStatusInput = createResult.ContinuationToken.ToDeploymentStatusInput();
            Assert.NotNull(createDeploymentStatusInput);
            Assert.NotNull(createDeploymentStatusInput.AzureDeploymentName);
            Assert.Equal(rgName, createDeploymentStatusInput.AzureResourceGroupName);
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
            var fileShareInfo = new ShareConnectionInfo("storageAccountName1",
                                                        "storageAccountKey1",
                                                        "storageShare1",
                                                        "storageFileName1");
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
            var timerAllocate = Stopwatch.StartNew();
            VirtualMachineProviderStartComputeResult startComputeResult = await computeProvider.StartComputeAsync(startComputeInput);
            timerAllocate.Stop();
            System.Console.WriteLine($"Time taken to allocate VM {timerAllocate.Elapsed.TotalSeconds}");
            Assert.NotNull(startComputeResult);
        }
    }
}