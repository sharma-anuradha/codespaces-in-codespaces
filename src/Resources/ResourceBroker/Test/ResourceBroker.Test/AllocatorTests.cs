using AutoMapper;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Strategies;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;
using ResourceType = Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.ResourceType;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Test
{
    public class AllocatorTests
    {
        [Theory]
        [InlineData(ResourceType.ComputeVM, true)]
        [InlineData(ResourceType.KeyVault, true)]
        [InlineData(ResourceType.StorageArchive, true)]
        [InlineData(ResourceType.StorageFileShare, true)]
        [InlineData(ResourceType.OSDisk, false)]
        public void BasicAllocate_CanHandle(ResourceType resourceType, bool expectedResult)
        {
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePoolManager = new Mock<IResourcePoolManager>().Object;
            var resourcePoolDefinitionStore = new Mock<IResourcePoolDefinitionStore>().Object;
            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>().Object;
            var taskHelper = new Mock<ITaskHelper>().Object;
            var mapper = new Mock<IMapper>().Object;

            var allocStrategy = new AllocationBasicStrategy(
                resourceRepository,
                resourcePoolManager,
                resourcePoolDefinitionStore,
                resourceContinuationOperations,
                taskHelper,
                mapper);

            var allocateInput = new AllocateInput()
            {
                Type = resourceType,
                Location = VsSaaS.Common.AzureLocation.WestUs2,
            };

            Assert.Equal(expectedResult, allocStrategy.CanHandle(new List<AllocateInput>() { allocateInput }));
        }

        [Fact]
        public void OSDiskAllocate_CanHandle()
        {
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePoolManager = new Mock<IResourcePoolManager>().Object;
            var resourcePoolDefinitionStore = new Mock<IResourcePoolDefinitionStore>().Object;
            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>().Object;
            var taskHelper = new Mock<ITaskHelper>().Object;
            var mapper = new Mock<IMapper>().Object;
            var diskProvider = new Mock<IDiskProvider>().Object;
            var agentSettings = new AgentSettings()
            {
                MinimumVersion = "1.2.3.4"
            };
            var computeProvider = new Mock<IComputeProvider>().Object;

            var allocStrategy = new AllocationOSDiskStrategy(
                resourceRepository,
                resourcePoolManager,
                resourcePoolDefinitionStore,
                resourceContinuationOperations,
                taskHelper,
                mapper,
                diskProvider,
                agentSettings,
                computeProvider);

            var allocateInputOSDisk = new AllocateInput()
            {
                Type = ResourceType.OSDisk,
                Location = VsSaaS.Common.AzureLocation.WestUs2,
            };

            var allocateInputVM = new AllocateInput()
            {
                Type = ResourceType.ComputeVM,
                Location = VsSaaS.Common.AzureLocation.WestUs2,
            };

            Assert.True(allocStrategy.CanHandle(new List<AllocateInput>() { allocateInputOSDisk, allocateInputVM }));

            Assert.False(allocStrategy.CanHandle(new List<AllocateInput>() { allocateInputOSDisk, allocateInputVM, allocateInputOSDisk, allocateInputVM }));
        }

        [Fact]
        public void OSDiskSnapshotAllocate_CanHandle()
        {
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var clientFactory = new Mock<IAzureClientFactory>().Object;
            var taskHelper = new Mock<ITaskHelper>().Object;
            var mapper = new Mock<IMapper>().Object;

            var allocStrategy = new AllocationOSDiskSnapshotStrategy(
                resourceRepository,
                clientFactory,
                taskHelper,
                mapper);

            var extendedProperties = new AllocateExtendedProperties();

            var allocateInputOSDisk = new AllocateInput
            {
                Type = ResourceType.OSDisk,
                Location = VsSaaS.Common.AzureLocation.WestUs2,
                ExtendedProperties = extendedProperties,
            };

            var allocateInputSnapshot = new AllocateInput
            {
                Type = ResourceType.Snapshot,
                Location = VsSaaS.Common.AzureLocation.WestUs2,
                ExtendedProperties = extendedProperties,
            };

            // Should fail if no source resource ID is given
            Assert.False(allocStrategy.CanHandle(new List<AllocateInput>() { allocateInputOSDisk }));
            Assert.False(allocStrategy.CanHandle(new List<AllocateInput>() { allocateInputSnapshot }));
            Assert.False(allocStrategy.CanHandle(new List<AllocateInput>() { allocateInputOSDisk, allocateInputSnapshot }));

            // Should succeed if correct resource type and ID are sent
            allocateInputOSDisk.ExtendedProperties.OSDiskResourceID = Guid.NewGuid().ToString();
            allocateInputSnapshot.ExtendedProperties.OSDiskSnapshotResourceID = Guid.NewGuid().ToString();
            Assert.True(allocStrategy.CanHandle(new List<AllocateInput>() { allocateInputOSDisk }));
            Assert.True(allocStrategy.CanHandle(new List<AllocateInput>() { allocateInputSnapshot }));
            Assert.True(allocStrategy.CanHandle(new List<AllocateInput>() { allocateInputOSDisk, allocateInputSnapshot }));
        }
    }
}
