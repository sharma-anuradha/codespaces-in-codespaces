// <copyright file="ComputeVirtualMachineProviderTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
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
        private static readonly string ResourceName = ResourceId.ToString();
        private static readonly AzureResourceInfo ResourceInfo = new AzureResourceInfo(AzureSubscription, AzureResourceGroupName, ResourceName);

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
            var result = await computeProvider.CreateAsync(new VirtualMachineProviderCreateInput(), logger);
            ValidateVirtualMachineResult(result, OperationState.InProgress);
        }

        [Fact]
        public async Task VirtualMachine_Create_Initiate_Failed()
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.BeginCreateComputeAsync(It.IsAny<VirtualMachineProviderCreateInput>(), It.IsAny<IDiagnosticsLogger>()))
              .Returns(
                 Task.FromResult<(OperationState, NextStageInput)>(
                  (OperationState.Failed, default)));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            var result = await computeProvider.CreateAsync(new VirtualMachineProviderCreateInput(), logger);
            Assert.NotNull(result);
            Assert.Null(result.AzureResourceInfo);
            Assert.Equal(OperationState.Failed, result.Status);
            Assert.Null(result.NextInput);
        }

        [Fact]
        public async Task VirtualMachine_Continue_Create_InProgress()
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq
                .Setup(x => x.CheckCreateComputeStatusAsync(It.IsAny<NextStageInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                  Task.FromResult(
                    (OperationState.InProgress, CreateDeploymentState())));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            var continuationToken = new NextStageInput(TrackingId, ResourceInfo).ToJson();
            var result = await computeProvider.CreateAsync(new VirtualMachineProviderCreateInput { ContinuationToken = continuationToken }, logger);
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
            var deploymentStatusInput = new NextStageInput(TrackingId, ResourceInfo);
            var continuationToken = deploymentStatusInput.ToJson();
            deploymentManagerMoq.Setup(x => x.CheckCreateComputeStatusAsync(It.IsAny<NextStageInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                  Task.FromResult(
                    (outputState, deploymentStatusInput)));

            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            var result = await computeProvider.CreateAsync(new VirtualMachineProviderCreateInput { ContinuationToken = continuationToken }, logger);
            Assert.NotNull(result);
            Assert.Equal(expectedState, result.Status);
            Assert.Null(result.NextInput);
        }

        [Fact]
        public async Task VirtualMachine_StartCompute_Initiate_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.StartComputeAsync(It.IsAny<VirtualMachineProviderStartComputeInput>(), It.IsAny<int>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                   Task.FromResult(
                       (OperationState.InProgress, 0)));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            var result = await computeProvider.StartComputeAsync(CreateStartComputeInput(null), logger);
            Assert.NotNull(result);
            Assert.Equal(OperationState.InProgress, result.Status);
            Assert.NotNull(result.NextInput.ContinuationToken);
        }

        [Fact]
        public async Task VirtualMachine_Continue_StartCompute_InProgress()
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.StartComputeAsync(It.IsAny<VirtualMachineProviderStartComputeInput>(), It.IsAny<int>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                   Task.FromResult(
                       (OperationState.InProgress, 0)));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            var continuationToken = new NextStageInput(TrackingId, ResourceInfo).ToJson();
            var result = await computeProvider.StartComputeAsync(CreateStartComputeInput(continuationToken), logger);
            Assert.NotNull(result);
            Assert.Equal(OperationState.InProgress, result.Status);
            Assert.NotNull(result.NextInput.ContinuationToken);
        }

        [Theory]
        [InlineData(OperationState.Succeeded, OperationState.Succeeded)]
        [InlineData(OperationState.Cancelled, OperationState.Cancelled)]
        [InlineData(OperationState.Failed, OperationState.Failed)]
        public async Task VirtualMachine_Continue_StartCompute_Finished(OperationState outputState, OperationState expectedState)
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            var deploymentStatusInput = new NextStageInput(TrackingId, ResourceInfo);
            var continuationToken = deploymentStatusInput.ToJson();
            deploymentManagerMoq.Setup(x => x.StartComputeAsync(It.IsAny<VirtualMachineProviderStartComputeInput>(), It.IsAny<int>(), It.IsAny<IDiagnosticsLogger>()))
                 .Returns(
                    Task.FromResult(
                        (outputState, 0)));

            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            var result = await computeProvider.StartComputeAsync(CreateStartComputeInput(continuationToken), logger);
            Assert.NotNull(result);
            Assert.Equal(expectedState, result.Status);
        }

        [Fact]
        public async Task VirtualMachine_DeleteCompute_Initiate_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            deploymentManagerMoq.Setup(x => x.BeginDeleteComputeAsync(It.IsAny<VirtualMachineProviderDeleteInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                   Task.FromResult(
                       (OperationState.InProgress, CreateDeploymentState())));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            var result = await computeProvider.DeleteAsync(new VirtualMachineProviderDeleteInput() { AzureResourceInfo = ResourceInfo }, logger);
            ValidateVirtualMachineResult(result, OperationState.InProgress);
        }

        [Fact]
        public async Task VirtualMachine_Continue_DeleteCompute_InProgress()
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            var deploymentStatusInput = new NextStageInput(TrackingId, ResourceInfo);
            var continuationToken = deploymentStatusInput.ToJson();
            deploymentManagerMoq.Setup(x => x.CheckDeleteComputeStatusAsync(It.IsAny<NextStageInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(
                    (OperationState.InProgress, deploymentStatusInput)));
            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            var result = await computeProvider.DeleteAsync(new VirtualMachineProviderDeleteInput() { AzureResourceInfo = ResourceInfo, ContinuationToken = continuationToken }, logger);
            ValidateVirtualMachineResult(result, OperationState.InProgress);
        }

        [Theory]
        [InlineData(OperationState.Succeeded, OperationState.Succeeded)]
        [InlineData(OperationState.Cancelled, OperationState.Cancelled)]
        [InlineData(OperationState.Failed, OperationState.Failed)]
        public async Task VirtualMachine_Continue_DeleteCompute_Finished(OperationState outputState, OperationState expectedState)
        {
            var logger = new DefaultLoggerFactory().New();
            var deploymentManagerMoq = new Mock<IDeploymentManager>();
            var deploymentStatusInput = new NextStageInput(TrackingId, ResourceInfo);
            var continuationToken = deploymentStatusInput.ToJson();
            deploymentManagerMoq.Setup(x => x.CheckDeleteComputeStatusAsync(It.IsAny<NextStageInput>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(
                Task.FromResult(
                    (outputState, deploymentStatusInput)));

            var computeProvider = new VirtualMachineProvider(deploymentManagerMoq.Object);
            var result = await computeProvider.DeleteAsync(new VirtualMachineProviderDeleteInput() { AzureResourceInfo = ResourceInfo, ContinuationToken = continuationToken }, logger);
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
                new ShareConnectionInfo("val1", "val2", "val3", "val4", "val5"),
                new Dictionary<string, string>(),
                new HashSet<UserSecretData>(),
                ComputeOS.Linux,
                AzureLocation.WestUs2,
                "Standard_D4s_v3",
                continuationToken);
        }
    }
}
