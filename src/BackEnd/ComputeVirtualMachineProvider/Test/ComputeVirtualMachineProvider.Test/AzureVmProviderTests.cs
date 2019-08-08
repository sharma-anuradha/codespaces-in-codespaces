using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
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

        // [Fact(Skip = "Integration Test")]
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
                SkuName = "Standard_F4s_v2",
            };

            VirtualMachineProviderCreateResult createResult = await computeProvider.CreateAsync(input, null);
            Assert.NotNull(createResult);
            Assert.Equal("InProgress", createResult.Status);
            DeploymentStatusInput createDeploymentStatusToken = createResult.ContinuationToken.ToDeploymentStatusInput();
            Assert.NotNull(createDeploymentStatusToken);
            Assert.NotNull(createDeploymentStatusToken.AzureDeploymentName);
            Assert.Equal(rgName, createDeploymentStatusToken.AzureResourceGroupName);
            Assert.NotEqual(Guid.Empty, createDeploymentStatusToken.ResourceId.InstanceId);
            Assert.Equal(subscriptionId, createDeploymentStatusToken.ResourceId.SubscriptionId);
            Assert.Equal(eastUs, createDeploymentStatusToken.ResourceId.Location);


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

        }
    }
}