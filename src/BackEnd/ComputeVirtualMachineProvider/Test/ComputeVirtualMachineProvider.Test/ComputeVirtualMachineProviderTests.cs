// <copyright file="ComputeVirtualMachineProviderTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Test
{
    public class ComputeVirtualMachineProviderTests
    {
        private const string AzureSubscription = "TestSub1";
        private const string AzureResourceGroupName = "TestRG1";
        private const string AzureDeploymentName = "TestDeployment1";
        private const string ResourceId = "ResourceId1";

        [Fact]
        public void Ctor_with_bad_options()
        {
            Assert.Throws<ArgumentNullException>(() => new VirtualMachineProvider(null));
        }

        [Fact]
        public async Task VirtualMachine_Create_Initiate_Ok()
        {
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.BeginCreateAsync(It.IsAny<VirtualMachineInstance>())).Returns(Task.FromResult(CreateDeploymentState()));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderCreateResult result = await computeProvider.CreateAsync(new VirtualMachineProviderCreateInput(), null);
            ValidateVirtualMachineCreateResult(result);
        }

        [Fact]
        public async Task VirtualMachine_Continue_Create_InProgress()
        {
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.CheckDeploymentStatusAsync(It.IsAny<DeploymentStatusInput>())).Returns(Task.FromResult(DeploymentState.InProgress));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            string continuationToken = new DeploymentStatusInput(AzureSubscription, AzureResourceGroupName, AzureDeploymentName, ResourceId).ToJson();
            VirtualMachineProviderCreateResult result = await computeProvider.CreateAsync(new VirtualMachineProviderCreateInput(), continuationToken);
            ValidateVirtualMachineCreateResult(result);
        }

        [Theory]
        [InlineData(DeploymentState.Succeeded, "Succeeded")]
        [InlineData(DeploymentState.Cancelled, "Cancelled")]
        [InlineData(DeploymentState.Failed, "Failed")]
        public async Task VirtualMachine_Continue_Create_Finished(DeploymentState outputState, string expectedState)
        {
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.CheckDeploymentStatusAsync(It.IsAny<DeploymentStatusInput>())).Returns(Task.FromResult(outputState));

            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            string continuationToken = new DeploymentStatusInput(AzureSubscription, AzureResourceGroupName, AzureDeploymentName, ResourceId).ToJson();
            VirtualMachineProviderCreateResult result = await computeProvider.CreateAsync(new VirtualMachineProviderCreateInput(), continuationToken);
            Assert.NotNull(result);
            Assert.Equal(expectedState, result.Status);
            Assert.Equal(ResourceId, result.ResourceId);
            Assert.Null(result.ContinuationToken);
        }

        private static void ValidateVirtualMachineCreateResult(VirtualMachineProviderCreateResult result)
        {
            Assert.NotNull(result);
            Assert.Equal("InProgress", result.Status);
            Assert.Equal(ResourceId, result.ResourceId);
            Assert.NotNull(result.ContinuationToken);
            var deploymentStatusInput = result.ContinuationToken.ToDeploymentStatusInput();
            Assert.NotNull(deploymentStatusInput);
            Assert.Equal(AzureSubscription, deploymentStatusInput.AzureSubscription);
            Assert.Equal(AzureResourceGroupName, deploymentStatusInput.AzureResourceGroupName);
            Assert.Equal(AzureDeploymentName, deploymentStatusInput.AzureDeploymentName);
            Assert.Equal(ResourceId, deploymentStatusInput.ResourceId);
        }

        private static DeploymentStatusInput CreateDeploymentState()
        {
            return new DeploymentStatusInput(AzureSubscription, AzureResourceGroupName, AzureDeploymentName, ResourceId);
        }
    }
}
