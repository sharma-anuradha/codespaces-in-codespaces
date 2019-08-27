// <copyright file="ComputeVirtualMachineProviderTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Test
{
    public class ComputeVirtualMachineProviderTests
    {
        private static Guid AzureSubscription = Guid.NewGuid();
        private const string AzureResourceGroupName = "TestRG1";
        private const string TrackingId = "TestDeployment1";
        private static ResourceId ResourceId = new ResourceId(ResourceType.ComputeVM, Guid.NewGuid(), AzureSubscription, AzureResourceGroupName, AzureLocation.EastUs);

        [Fact]
        public void Ctor_with_bad_options()
        {
            Assert.Throws<ArgumentNullException>(() => new VirtualMachineProvider(null));
        }

        [Fact]
        public async Task VirtualMachine_Create_Initiate_Ok()
        {
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.BeginCreateComputeAsync(It.IsAny<VirtualMachineProviderCreateInput>()))
                .Returns(
                   Task.FromResult(
                    (OperationState.InProgress, CreateDeploymentState())));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderCreateResult result = await computeProvider.CreateAsync(new VirtualMachineProviderCreateInput(), null);
            ValidateVirtualMachineResult<VirtualMachineProviderCreateResult>(result, OperationState.InProgress);
        }

        [Fact]
        public async Task VirtualMachine_Continue_Create_InProgress()
        {
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.CheckCreateComputeStatusAsync(It.IsAny<NextStageInput>()))
                .Returns(
                  Task.FromResult(
                    (OperationState.InProgress, CreateDeploymentState())));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            string continuationToken = new NextStageInput(TrackingId, ResourceId).ToJson();
            VirtualMachineProviderCreateResult result = await computeProvider.CreateAsync(new VirtualMachineProviderCreateInput(), continuationToken);
            ValidateVirtualMachineResult<VirtualMachineProviderCreateResult>(result, OperationState.InProgress);
        }

        [Theory]
        [InlineData(OperationState.Succeeded, OperationState.Succeeded)]
        [InlineData(OperationState.Cancelled, OperationState.Cancelled)]
        [InlineData(OperationState.Failed, OperationState.Failed)]
        public async Task VirtualMachine_Continue_Create_Finished(OperationState outputState, OperationState expectedState)
        {
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            NextStageInput deploymentStatusInput = new NextStageInput(TrackingId, ResourceId);
            string continuationToken = deploymentStatusInput.ToJson();
            deploymentManagerMoq.Setup(x => x.CheckCreateComputeStatusAsync(It.IsAny<NextStageInput>()))
                .Returns(
                  Task.FromResult(
                    (outputState, deploymentStatusInput)));

            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderCreateResult result = await computeProvider.CreateAsync(new VirtualMachineProviderCreateInput(), continuationToken);
            Assert.NotNull(result);
            Assert.Equal(expectedState, result.Status);
            Assert.Null(result.ContinuationToken);
        }

        [Fact]
        public async Task VirtualMachine_StartCompute_Initiate_Ok()
        {
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.BeginStartComputeAsync(It.IsAny<VirtualMachineProviderStartComputeInput>()))
                .Returns(
                   Task.FromResult(
                       (OperationState.InProgress, CreateDeploymentState())));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderStartComputeResult result = await computeProvider.StartComputeAsync(CreateStartComputeInput(), null);
            ValidateVirtualMachineResult<VirtualMachineProviderStartComputeResult>(result, OperationState.InProgress);
        }

        [Fact]
        public async Task VirtualMachine_Continue_StartCompute_InProgress()
        {
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.CheckStartComputeStatusAsync(It.IsAny<NextStageInput>()))
                .Returns(
                Task.FromResult(
                    (OperationState.InProgress, CreateDeploymentState())));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            string continuationToken = new NextStageInput(TrackingId, ResourceId).ToJson();
            VirtualMachineProviderStartComputeResult result = await computeProvider.StartComputeAsync(CreateStartComputeInput(), continuationToken);
            ValidateVirtualMachineResult<VirtualMachineProviderStartComputeResult>(result, OperationState.InProgress);
        }

        [Theory]
        [InlineData(OperationState.Succeeded, OperationState.Succeeded)]
        [InlineData(OperationState.Cancelled, OperationState.Cancelled)]
        [InlineData(OperationState.Failed, OperationState.Failed)]
        public async Task VirtualMachine_Continue_StartCompute_Finished(OperationState outputState, OperationState expectedState)
        {
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            NextStageInput deploymentStatusInput = new NextStageInput(TrackingId, ResourceId);
            string continuationToken = deploymentStatusInput.ToJson();
            deploymentManagerMoq.Setup(x => x.CheckStartComputeStatusAsync(It.IsAny<NextStageInput>()))
                .Returns(
                    Task.FromResult(
                        (outputState, deploymentStatusInput)));

            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderStartComputeResult result = await computeProvider.StartComputeAsync(CreateStartComputeInput(), continuationToken);
            Assert.NotNull(result);
            Assert.Equal(expectedState, result.Status);
            Assert.Null(result.ContinuationToken);
        }

        [Fact]
        public async Task VirtualMachine_DeleteCompute_Initiate_Ok()
        {
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.BeginDeleteComputeAsync(It.IsAny<VirtualMachineProviderDeleteInput>()))
                .Returns(
                   Task.FromResult(
                       (OperationState.InProgress,CreateDeploymentState())));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderDeleteResult result = await computeProvider.DeleteAsync(new VirtualMachineProviderDeleteInput(), null);
            ValidateVirtualMachineResult<VirtualMachineProviderDeleteResult>(result, OperationState.InProgress);
        }

        [Fact]
        public async Task VirtualMachine_Continue_DeleteCompute_InProgress()
        {
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            NextStageInput deploymentStatusInput = new NextStageInput(TrackingId, ResourceId);
            string continuationToken = deploymentStatusInput.ToJson();
            deploymentManagerMoq.Setup(x => x.CheckDeleteComputeStatusAsync(It.IsAny<NextStageInput>()))
                .Returns(Task.FromResult(
                    (OperationState.InProgress, deploymentStatusInput)));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderDeleteResult result = await computeProvider.DeleteAsync(new VirtualMachineProviderDeleteInput(), continuationToken);
            ValidateVirtualMachineResult<VirtualMachineProviderDeleteResult>(result, OperationState.InProgress);
        }

        [Theory]
        [InlineData(OperationState.Succeeded, OperationState.Succeeded)]
        [InlineData(OperationState.Cancelled, OperationState.Cancelled)]
        [InlineData(OperationState.Failed, OperationState.Failed)]
        public async Task VirtualMachine_Continue_Deleteompute_Finished(OperationState outputState, OperationState expectedState)
        {
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            NextStageInput deploymentStatusInput = new NextStageInput(TrackingId, ResourceId);
            string continuationToken = deploymentStatusInput.ToJson();
            deploymentManagerMoq.Setup(x => x.CheckDeleteComputeStatusAsync(It.IsAny<NextStageInput>()))
                .Returns(
                Task.FromResult(
                    (outputState, deploymentStatusInput)));

            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderDeleteResult result = await computeProvider.DeleteAsync(new VirtualMachineProviderDeleteInput(), continuationToken);
            Assert.NotNull(result);
            Assert.Equal(expectedState, result.Status);
            Assert.Null(result.ContinuationToken);
        }

        private static void ValidateVirtualMachineResult<T>(T result, OperationState expectedState) where T : ContinuationResult
        {
            Assert.NotNull(result);
            Assert.Equal(expectedState, result.Status);
            Assert.NotNull(result.ContinuationToken);
            var deploymentStatusInput = result.ContinuationToken.ToDeploymentStatusInput();
            Assert.NotNull(deploymentStatusInput);
            Assert.Equal(ResourceId, deploymentStatusInput.ResourceId);
            Assert.Equal(AzureSubscription, deploymentStatusInput.ResourceId.SubscriptionId);
            Assert.Equal(AzureResourceGroupName, deploymentStatusInput.ResourceId.ResourceGroup);
            Assert.Equal(TrackingId, deploymentStatusInput.TrackingId);
            Assert.Equal(ResourceId, deploymentStatusInput.ResourceId);
        }

        private static NextStageInput CreateDeploymentState()
        {
            return new NextStageInput(TrackingId, ResourceId);
        }

        private static VirtualMachineProviderStartComputeInput CreateStartComputeInput()
        {
            return new VirtualMachineProviderStartComputeInput(
                ResourceId,
                new ShareConnectionInfo("val1", "val2", "val3", "val4"),
                new System.Collections.Generic.Dictionary<string, string>());
        }
    }
}
