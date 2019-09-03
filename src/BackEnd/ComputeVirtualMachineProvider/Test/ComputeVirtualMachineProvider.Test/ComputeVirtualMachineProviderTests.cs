// <copyright file="ComputeVirtualMachineProviderTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
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
        private static Guid ResourceId = Guid.NewGuid();
        private static string ResourceName = ResourceId.ToString();
        private static AzureResourceInfo ResourceInfo = new AzureResourceInfo(AzureSubscription, AzureResourceGroupName, ResourceName);

        [Fact]
        public void Ctor_with_bad_options()
        {
            Assert.Throws<ArgumentNullException>(() => new VirtualMachineProvider(null));
        }

        [Fact]
        public async Task VirtualMachine_Create_Initiate_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.BeginCreateComputeAsync(It.IsAny<VirtualMachineProviderCreateInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                   Task.FromResult(
                    (OperationState.InProgress, CreateDeploymentState())));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderCreateResult result = await computeProvider.CreateAsync(new VirtualMachineProviderCreateInput(), logger);
            ValidateVirtualMachineResult(result, OperationState.InProgress);
        }

        [Fact]
        public async Task VirtualMachine_Continue_Create_InProgress()
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.CheckCreateComputeStatusAsync(It.IsAny<NextStageInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                  Task.FromResult(
                    (OperationState.InProgress, CreateDeploymentState())));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            string continuationToken = new NextStageInput(TrackingId, ResourceInfo).ToJson();
            VirtualMachineProviderCreateResult result = await computeProvider.CreateAsync(new VirtualMachineProviderCreateInput { ContinuationToken = continuationToken }, logger);
            ValidateVirtualMachineResult(result, OperationState.InProgress);
        }

        [Theory]
        [InlineData(OperationState.Succeeded, OperationState.Succeeded)]
        [InlineData(OperationState.Cancelled, OperationState.Cancelled)]
        [InlineData(OperationState.Failed, OperationState.Failed)]
        public async Task VirtualMachine_Continue_Create_Finished(OperationState outputState, OperationState expectedState)
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            NextStageInput deploymentStatusInput = new NextStageInput(TrackingId, ResourceInfo);
            string continuationToken = deploymentStatusInput.ToJson();
            deploymentManagerMoq.Setup(x => x.CheckCreateComputeStatusAsync(It.IsAny<NextStageInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                  Task.FromResult(
                    (outputState, deploymentStatusInput)));

            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderCreateResult result = await computeProvider.CreateAsync(new VirtualMachineProviderCreateInput { ContinuationToken = continuationToken }, logger);
            Assert.NotNull(result);
            Assert.Equal(expectedState, result.Status);
            Assert.Null(result.NextInput);
        }

        [Fact]
        public async Task VirtualMachine_StartCompute_Initiate_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.BeginStartComputeAsync(It.IsAny<VirtualMachineProviderStartComputeInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                   Task.FromResult(
                       (OperationState.InProgress, CreateDeploymentState())));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderStartComputeResult result = await computeProvider.StartComputeAsync(CreateStartComputeInput(null), logger);
            ValidateVirtualMachineResult(result, OperationState.InProgress);
        }

        [Fact]
        public async Task VirtualMachine_Continue_StartCompute_InProgress()
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.CheckStartComputeStatusAsync(It.IsAny<NextStageInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                Task.FromResult(
                    (OperationState.InProgress, CreateDeploymentState())));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            string continuationToken = new NextStageInput(TrackingId, ResourceInfo).ToJson();
            VirtualMachineProviderStartComputeResult result = await computeProvider.StartComputeAsync(CreateStartComputeInput(continuationToken), logger);
            ValidateVirtualMachineResult(result, OperationState.InProgress);
        }

        [Theory]
        [InlineData(OperationState.Succeeded, OperationState.Succeeded)]
        [InlineData(OperationState.Cancelled, OperationState.Cancelled)]
        [InlineData(OperationState.Failed, OperationState.Failed)]
        public async Task VirtualMachine_Continue_StartCompute_Finished(OperationState outputState, OperationState expectedState)
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            NextStageInput deploymentStatusInput = new NextStageInput(TrackingId, ResourceInfo);
            string continuationToken = deploymentStatusInput.ToJson();
            deploymentManagerMoq.Setup(x => x.CheckStartComputeStatusAsync(It.IsAny<NextStageInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                    Task.FromResult(
                        (outputState, deploymentStatusInput)));

            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderStartComputeResult result = await computeProvider.StartComputeAsync(CreateStartComputeInput(continuationToken), logger);
            Assert.NotNull(result);
            Assert.Equal(expectedState, result.Status);
            Assert.Null(result.NextInput);
        }

        [Fact]
        public async Task VirtualMachine_DeleteCompute_Initiate_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.BeginDeleteComputeAsync(It.IsAny<VirtualMachineProviderDeleteInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                   Task.FromResult(
                       (OperationState.InProgress,CreateDeploymentState())));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderDeleteResult result = await computeProvider.DeleteAsync(new VirtualMachineProviderDeleteInput() { AzureResourceInfo = ResourceInfo }, logger);
            ValidateVirtualMachineResult(result, OperationState.InProgress);
        }

        [Fact]
        public async Task VirtualMachine_Continue_DeleteCompute_InProgress()
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            NextStageInput deploymentStatusInput = new NextStageInput(TrackingId, ResourceInfo);
            string continuationToken = deploymentStatusInput.ToJson();
            deploymentManagerMoq.Setup(x => x.CheckDeleteComputeStatusAsync(It.IsAny<NextStageInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(
                    (OperationState.InProgress, deploymentStatusInput)));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderDeleteResult result = await computeProvider.DeleteAsync(new VirtualMachineProviderDeleteInput() { AzureResourceInfo= ResourceInfo, ContinuationToken = continuationToken }, logger);
            ValidateVirtualMachineResult(result, OperationState.InProgress);
        }

        [Theory]
        [InlineData(OperationState.Succeeded, OperationState.Succeeded)]
        [InlineData(OperationState.Cancelled, OperationState.Cancelled)]
        [InlineData(OperationState.Failed, OperationState.Failed)]
        public async Task VirtualMachine_Continue_Deleteompute_Finished(OperationState outputState, OperationState expectedState)
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            NextStageInput deploymentStatusInput = new NextStageInput(TrackingId, ResourceInfo);
            string continuationToken = deploymentStatusInput.ToJson();
            deploymentManagerMoq.Setup(x => x.CheckDeleteComputeStatusAsync(It.IsAny<NextStageInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                Task.FromResult(
                    (outputState, deploymentStatusInput)));

            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            VirtualMachineProviderDeleteResult result = await computeProvider.DeleteAsync(new VirtualMachineProviderDeleteInput() { AzureResourceInfo = ResourceInfo, ContinuationToken = continuationToken }, logger);
            Assert.NotNull(result);
            Assert.Equal(expectedState, result.Status);
            Assert.Null(result.NextInput);
        }

        private static void ValidateVirtualMachineResult<T>(T result, OperationState expectedState) where T : ContinuationResult
        {
            Assert.NotNull(result);
            Assert.Equal(expectedState, result.Status);
            Assert.NotNull(result.NextInput.ContinuationToken);
            var deploymentStatusInput = result.NextInput.ContinuationToken.ToNextStageInput();
            Assert.NotNull(deploymentStatusInput);
            Assert.Equal(ResourceInfo, deploymentStatusInput.AzureResourceInfo);
            Assert.Equal(TrackingId, deploymentStatusInput.TrackingId);
        }

        private static NextStageInput CreateDeploymentState()
        {
            return new NextStageInput(TrackingId, ResourceInfo);
        }

        private static VirtualMachineProviderStartComputeInput CreateStartComputeInput(string continuationToken)
        {
            return new VirtualMachineProviderStartComputeInput(
                ResourceInfo,
                new ShareConnectionInfo("val1", "val2", "val3", "val4"),
                new System.Collections.Generic.Dictionary<string, string>(),
                continuationToken);
        }
    }
}
