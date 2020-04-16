// <copyright file="ResourceBrokerTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using Xunit;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using System.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Test
{
    public class ResourceBrokerTests
    {
        private Guid EnvironmentId = Guid.NewGuid();
        private Guid ResourceId1 = Guid.NewGuid();
        private Guid ResourceId2 = Guid.NewGuid();
        private string Reason = "Test";
        private string DefaultPoolCode = "PoolCode";
        private const AzureLocation DefaultLocation = AzureLocation.EastUs;
        private const AzureLocation WestLocation = AzureLocation.WestUs2;
        private const string DefaultResourceSkuName = "LargeVm";
        private const string DefaultLogicalSkuName = "Large";
        private const string StorageResourceSkuName = "LargeVm";
        private const ResourceType DefaultType = ResourceType.ComputeVM;

        /*
        private const string DefaultLocation = "USW2";
         */

        [Fact]
        public void Ctor_throws_if_null()
        {
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePool = new Mock<IResourcePoolManager>().Object;
            var resourceScalingStore = new Mock<IResourcePoolDefinitionStore>().Object;
            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>().Object;
            var taskHelper = new Mock<ITaskHelper>().Object;
            var mapper = new Mock<IMapper>().Object;

            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, null, null, null, null, mapper));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, null, null, null, taskHelper, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, null, null, resourceContinuationOperations, null, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, null, resourceScalingStore, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, resourcePool, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(resourceRepository, null, null, null, null, null));
        }

        [Fact]
        public void Ctor_ok()
        {
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePool = new Mock<IResourcePoolManager>().Object;
            var resourceScalingStore = new Mock<IResourcePoolDefinitionStore>().Object;
            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>().Object;
            var taskHelper = new Mock<ITaskHelper>().Object;
            var mapper = new Mock<IMapper>().Object;

            var provider = new ResourceBroker(resourceRepository, resourcePool, resourceScalingStore, resourceContinuationOperations, taskHelper, mapper);

            Assert.NotNull(provider);
        }

        [Fact()]
        public async void ResourceBroker_Allocate_WhenHasCapacity_ReturnsResource()
        {
            var input = BuildAllocateInput();
            var rawResult = BuildResourceRecord();

            var scalingStore = BuildResourceScalingStore();
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>().Object;
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePool = new Mock<IResourcePoolManager>();
            resourcePool.Setup(x => x.TryGetAsync(DefaultPoolCode, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(rawResult));

            var provider = new ResourceBroker(resourceRepository, resourcePool.Object, scalingStore.Object, resourceContinuationOperations, taskHelper, mapper);
            var result = await provider.AllocateAsync(EnvironmentId, input, Reason, logger);

            Assert.NotNull(result);
            Assert.Equal(rawResult.Created, result.Created);
            Assert.Equal(rawResult.SkuName, result.SkuName);
            Assert.Equal(rawResult.Type, result.Type);
        }

        [Fact]
        public void ResourceBroker_Allocate_WhenHasNoCapacity_ReturnsNull()
        {
            var input = BuildAllocateInput();

            var scalingStore = BuildResourceScalingStore();
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>().Object;
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePool = new Mock<IResourcePoolManager>();
            resourcePool.Setup(x => x.TryGetAsync(DefaultPoolCode, logger)).Returns(Task.FromResult((ResourceRecord)null));

            var provider = new ResourceBroker(resourceRepository, resourcePool.Object, scalingStore.Object, resourceContinuationOperations, taskHelper, mapper);

            Assert.ThrowsAsync<OutOfCapacityException>(async () => await provider.AllocateAsync(EnvironmentId, input, Reason, logger));
        }

        [Fact]
        public void ResourceBroker_Allocate_WhenNoLogicalToResourceSkuMatchOccurs_ThrowsException()
        {
            var input = BuildAllocateInput();

            var scalingStore = BuildResourceScalingStore(true);
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>().Object;
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePool = new Mock<IResourcePoolManager>();
            resourcePool.Setup(x => x.TryGetAsync(DefaultPoolCode, logger)).Returns(Task.FromResult((ResourceRecord)null));
            
            var provider = new ResourceBroker(resourceRepository, resourcePool.Object, scalingStore.Object, resourceContinuationOperations, taskHelper, mapper);

            Assert.ThrowsAsync<ArgumentException>(async () => await provider.AllocateAsync(EnvironmentId, input, Reason, logger));
        }

        [Fact]
        public void ResourceBroker_Start_NotImplemented_ThrowsException()
        {
            var input = BuildStartInput(ResourceId1);

            var scalingStore = BuildResourceScalingStore(true);
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>().Object;
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePool = new Mock<IResourcePoolManager>().Object;

            var provider = new ResourceBroker(resourceRepository, resourcePool, scalingStore.Object, resourceContinuationOperations, taskHelper, mapper);

            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.StartAsync(EnvironmentId, StartAction.StartArchive, input, Reason, logger));
            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.StartAsync(EnvironmentId, StartAction.StartCompute, input, Reason, logger));
        }

        [Fact]
        public async void ResourceBroker_StartSet_StartCompute()
        {
            var variables = new Dictionary<string, string>();
            var input1 = BuildStartInput(ResourceId1);
            input1.Variables = variables;
            var input2 = BuildStartInput(ResourceId2);

            var scalingStore = BuildResourceScalingStore(true);
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>();
            resourceContinuationOperations.Setup(x => x.StartEnvironmentAsync(EnvironmentId, input1.ResourceId, input2.ResourceId, null, variables, Reason, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new ContinuationResult())).Verifiable();
            var resourceRepository = new Mock<IResourceRepository>();
            resourceRepository.Setup(x => x.GetAsync(ResourceId1.ToString(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new ResourceRecord() { Id = ResourceId1.ToString(), Type = ResourceType.ComputeVM })).Verifiable();
            resourceRepository.Setup(x => x.GetAsync(ResourceId2.ToString(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new ResourceRecord() { Id = ResourceId2.ToString(), Type = ResourceType.StorageFileShare })).Verifiable();
            var resourcePool = new Mock<IResourcePoolManager>().Object;

            var provider = new ResourceBroker(resourceRepository.Object, resourcePool, scalingStore.Object, resourceContinuationOperations.Object, taskHelper, mapper);
            var result = await provider.StartAsync(EnvironmentId, StartAction.StartCompute, new List<StartInput> { input1, input2 }, Reason, logger);

            Assert.True(result);
            resourceContinuationOperations.Verify();
        }

        [Fact]
        public void ResourceBroker_StartSet_StartCompute_ThrowsException_WhenNotTwoInput()
        {
            var input = BuildStartInput(ResourceId1);

            var scalingStore = BuildResourceScalingStore(true);
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>().Object;
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePool = new Mock<IResourcePoolManager>().Object;

            var provider = new ResourceBroker(resourceRepository, resourcePool, scalingStore.Object, resourceContinuationOperations, taskHelper, mapper);

            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.StartAsync(EnvironmentId, StartAction.StartCompute, new List<StartInput>() { input }, Reason, logger));
            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.StartAsync(EnvironmentId, StartAction.StartCompute, new List<StartInput>() { input, input, input }, Reason, logger));
            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.StartAsync(EnvironmentId, StartAction.StartCompute, new List<StartInput>(), Reason, logger));
            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.StartAsync(EnvironmentId, StartAction.StartCompute, (IList<StartInput>)null, Reason, logger));
        }

        [Fact]
        public async void ResourceBroker_StartSet_StartArchive()
        {
            var input1 = BuildStartInput(ResourceId1);
            var input2 = BuildStartInput(ResourceId2);

            var scalingStore = BuildResourceScalingStore(true);
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>();
            resourceContinuationOperations.Setup(x => x.StartArchiveAsync(EnvironmentId, input1.ResourceId, input2.ResourceId, Reason, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new ContinuationResult())).Verifiable();
            var resourceRepository = new Mock<IResourceRepository>();
            resourceRepository.Setup(x => x.GetAsync(ResourceId1.ToString(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new ResourceRecord() { Id = ResourceId1.ToString(), Type = ResourceType.StorageArchive })).Verifiable();
            resourceRepository.Setup(x => x.GetAsync(ResourceId2.ToString(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new ResourceRecord() { Id = ResourceId2.ToString(), Type = ResourceType.StorageFileShare })).Verifiable();
            var resourcePool = new Mock<IResourcePoolManager>().Object;

            var provider = new ResourceBroker(resourceRepository.Object, resourcePool, scalingStore.Object, resourceContinuationOperations.Object, taskHelper, mapper);
            var result = await provider.StartAsync(EnvironmentId, StartAction.StartArchive, new List<StartInput> { input1, input2 }, Reason, logger);

            Assert.True(result);
            resourceContinuationOperations.Verify();
        }

        [Fact]
        public void ResourceBroker_StartSet_StartArchive_ThrowsException_WhenNotTwoInput()
        {
            var input = BuildStartInput(ResourceId1);

            var scalingStore = BuildResourceScalingStore(true);
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>().Object;
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePool = new Mock<IResourcePoolManager>().Object;

            var provider = new ResourceBroker(resourceRepository, resourcePool, scalingStore.Object, resourceContinuationOperations, taskHelper, mapper);

            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.StartAsync(EnvironmentId, StartAction.StartArchive, new List<StartInput>() { input }, Reason, logger));
            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.StartAsync(EnvironmentId, StartAction.StartArchive, new List<StartInput>() { input, input, input }, Reason, logger));
            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.StartAsync(EnvironmentId, StartAction.StartArchive, new List<StartInput>(), Reason, logger));
            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.StartAsync(EnvironmentId, StartAction.StartCompute, (IList<StartInput>)null, Reason, logger));
        }

        [Fact]
        public async void ResourceBroker_Suspend_Success()
        {
            var input = BuildSuspendInput(ResourceId1);

            var scalingStore = BuildResourceScalingStore(true);
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>();
            resourceContinuationOperations.Setup(x => x.SuspendAsync(EnvironmentId, ResourceId1, Reason, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new ContinuationResult())).Verifiable();
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePool = new Mock<IResourcePoolManager>().Object;

            var provider = new ResourceBroker(resourceRepository, resourcePool, scalingStore.Object, resourceContinuationOperations.Object, taskHelper, mapper);
            var result = await provider.SuspendAsync(EnvironmentId, input, Reason, logger);

            Assert.True(result);
            resourceContinuationOperations.Verify();
        }

        [Fact]
        public async void ResourceBroker_SuspendSet_Success()
        {
            var input1 = BuildSuspendInput(ResourceId1);
            var input2 = BuildSuspendInput(ResourceId2);

            var scalingStore = BuildResourceScalingStore(true);
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>();
            resourceContinuationOperations.Setup(x => x.SuspendAsync(EnvironmentId, ResourceId1, Reason, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new ContinuationResult())).Verifiable();
            resourceContinuationOperations.Setup(x => x.SuspendAsync(EnvironmentId, ResourceId2, Reason, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new ContinuationResult())).Verifiable();
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePool = new Mock<IResourcePoolManager>().Object;

            var provider = new ResourceBroker(resourceRepository, resourcePool, scalingStore.Object, resourceContinuationOperations.Object, taskHelper, mapper);
            var result = await provider.SuspendAsync(EnvironmentId, new List<SuspendInput>() { input1, input2 }, Reason, logger);

            Assert.True(result);
            resourceContinuationOperations.Verify();
        }

        [Fact]
        public async void ResourceBroker_Delete_Success()
        {
            var input = BuildDeleteInput(ResourceId1);

            var scalingStore = BuildResourceScalingStore(true);
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>();
            resourceContinuationOperations.Setup(x => x.SuspendAsync(EnvironmentId, ResourceId1, Reason, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new ContinuationResult())).Verifiable();
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePool = new Mock<IResourcePoolManager>().Object;

            var provider = new ResourceBroker(resourceRepository, resourcePool, scalingStore.Object, resourceContinuationOperations.Object, taskHelper, mapper);
            var result = await provider.DeleteAsync(EnvironmentId, input, Reason, logger);

            Assert.True(result);
        }

        [Fact]
        public async void ResourceBroker_DeleteSet_Success()
        {
            var input1 = BuildDeleteInput(ResourceId1);
            var input2 = BuildDeleteInput(ResourceId2);

            var scalingStore = BuildResourceScalingStore(true);
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>();
            resourceContinuationOperations.Setup(x => x.DeleteAsync(EnvironmentId, ResourceId1, Reason, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new ContinuationResult())).Verifiable();
            resourceContinuationOperations.Setup(x => x.DeleteAsync(EnvironmentId, ResourceId2, Reason, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(new ContinuationResult())).Verifiable();
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourcePool = new Mock<IResourcePoolManager>().Object;

            var provider = new ResourceBroker(resourceRepository, resourcePool, scalingStore.Object, resourceContinuationOperations.Object, taskHelper, mapper);
            var result = await provider.DeleteAsync(EnvironmentId, new List<DeleteInput>() { input1, input2 }, Reason, logger);

            Assert.True(result);
            resourceContinuationOperations.Verify();
        }

        [Fact]
        public async void ResourceBroker_Status_Success()
        {
            var input = BuildStatusInput(ResourceId1);
            var resource = new ResourceRecord()
            {
                Id = ResourceId1.ToString(),
                ProvisioningStatus = OperationState.Cancelled,
                ProvisioningStatusChanged = DateTime.Now.AddDays(-3),
                StartingStatus = OperationState.Failed,
                StartingStatusChanged = DateTime.Now.AddDays(-2),
                DeletingStatus = OperationState.Initialized,
                DeletingStatusChanged = DateTime.Now.AddDays(-1),
                CleanupStatus = OperationState.InProgress,
                CleanupStatusChanged = DateTime.Now,
                SkuName = DefaultResourceSkuName,
                Location = DefaultLocation.ToString(),
            };

            var scalingStore = BuildResourceScalingStore(true);
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>();
            var resourceRepository = new Mock<IResourceRepository>();
            resourceRepository.Setup(x => x.GetAsync(ResourceId1.ToString(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(resource)).Verifiable();
            var resourcePool = new Mock<IResourcePoolManager>().Object;

            var provider = new ResourceBroker(resourceRepository.Object, resourcePool, scalingStore.Object, resourceContinuationOperations.Object, taskHelper, mapper);
            var result = await provider.StatusAsync(EnvironmentId, input, Reason, logger);

            Assert.Equal(resource.Id, result.ResourceId.ToString());
            Assert.Equal(resource.ProvisioningStatus, result.ProvisioningStatus);
            Assert.Equal(resource.ProvisioningStatusChanged, result.ProvisioningStatusChanged);
            Assert.Equal(resource.StartingStatus, result.StartingStatus);
            Assert.Equal(resource.StartingStatusChanged, result.StartingStatusChanged);
            Assert.Equal(resource.DeletingStatus, result.DeletingStatus);
            Assert.Equal(resource.DeletingStatusChanged, result.DeletingStatusChanged);
            Assert.Equal(resource.CleanupStatus, result.CleanupStatus);
            Assert.Equal(resource.CleanupStatusChanged, result.CleanupStatusChanged);
            Assert.Equal(resource.SkuName, result.SkuName);
            Assert.Equal(resource.Location, result.Location.ToString());
            resourceRepository.Verify();
        }

        [Fact]
        public async void ResourceBroker_StatusSet_Success()
        {
            var input1 = BuildStatusInput(ResourceId1);
            var resource1 = new ResourceRecord()
            {
                Id = ResourceId1.ToString(),
                ProvisioningStatus = OperationState.Cancelled,
                ProvisioningStatusChanged = DateTime.Now.AddDays(-3),
                StartingStatus = OperationState.Failed,
                StartingStatusChanged = DateTime.Now.AddDays(-2),
                DeletingStatus = OperationState.Initialized,
                DeletingStatusChanged = DateTime.Now.AddDays(-1),
                CleanupStatus = OperationState.InProgress,
                CleanupStatusChanged = DateTime.Now,
                SkuName = DefaultResourceSkuName,
                Location = DefaultLocation.ToString(),
            };
            var input2 = BuildStatusInput(ResourceId2);
            var resource2 = new ResourceRecord()
            {
                Id = ResourceId2.ToString(),
                ProvisioningStatus = OperationState.InProgress,
                ProvisioningStatusChanged = DateTime.Now.AddDays(-30),
                StartingStatus = OperationState.Initialized,
                StartingStatusChanged = DateTime.Now.AddDays(-20),
                DeletingStatus = OperationState.Failed,
                DeletingStatusChanged = DateTime.Now.AddDays(-10),
                CleanupStatus = OperationState.Cancelled,
                CleanupStatusChanged = DateTime.Now.AddDays(-40),
                SkuName = StorageResourceSkuName,
                Location = WestLocation.ToString(),
            };

            var scalingStore = BuildResourceScalingStore(true);
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = BuildLogger().Object;

            var resourceContinuationOperations = new Mock<IResourceContinuationOperations>();
            var resourceRepository = new Mock<IResourceRepository>();
            resourceRepository.Setup(x => x.GetAsync(ResourceId1.ToString(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(resource1)).Verifiable();
            resourceRepository.Setup(x => x.GetAsync(ResourceId2.ToString(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(resource2)).Verifiable();
            var resourcePool = new Mock<IResourcePoolManager>().Object;

            var provider = new ResourceBroker(resourceRepository.Object, resourcePool, scalingStore.Object, resourceContinuationOperations.Object, taskHelper, mapper);
            var resultList = await provider.StatusAsync(EnvironmentId, new List<StatusInput>() { input1, input2 }, Reason, logger);

            Assert.Equal(2, resultList.Count());

            var result1 = resultList.Where(x => x.ResourceId == ResourceId1).Single();
            var result2 = resultList.Where(x => x.ResourceId == ResourceId2).Single();

            Assert.Equal(resource1.Id, result1.ResourceId.ToString());
            Assert.Equal(resource1.ProvisioningStatus, result1.ProvisioningStatus);
            Assert.Equal(resource1.ProvisioningStatusChanged, result1.ProvisioningStatusChanged);
            Assert.Equal(resource1.StartingStatus, result1.StartingStatus);
            Assert.Equal(resource1.StartingStatusChanged, result1.StartingStatusChanged);
            Assert.Equal(resource1.DeletingStatus, result1.DeletingStatus);
            Assert.Equal(resource1.DeletingStatusChanged, result1.DeletingStatusChanged);
            Assert.Equal(resource1.CleanupStatus, result1.CleanupStatus);
            Assert.Equal(resource1.CleanupStatusChanged, result1.CleanupStatusChanged);
            Assert.Equal(resource1.SkuName, result1.SkuName);
            Assert.Equal(resource1.Location, result1.Location.ToString());

            Assert.Equal(resource2.Id, result2.ResourceId.ToString());
            Assert.Equal(resource2.ProvisioningStatus, result2.ProvisioningStatus);
            Assert.Equal(resource2.ProvisioningStatusChanged, result2.ProvisioningStatusChanged);
            Assert.Equal(resource2.StartingStatus, result2.StartingStatus);
            Assert.Equal(resource2.StartingStatusChanged, result2.StartingStatusChanged);
            Assert.Equal(resource2.DeletingStatus, result2.DeletingStatus);
            Assert.Equal(resource2.DeletingStatusChanged, result2.DeletingStatusChanged);
            Assert.Equal(resource2.CleanupStatus, result2.CleanupStatus);
            Assert.Equal(resource2.CleanupStatusChanged, result2.CleanupStatusChanged);
            Assert.Equal(resource2.SkuName, result2.SkuName);
            Assert.Equal(resource2.Location, result2.Location.ToString());

            resourceRepository.Verify();
        }

        private Mock<IResourcePoolDefinitionStore> BuildResourceScalingStore(bool populateEmpty = false)
        {
            var definition = new List<ResourcePool>();
            if (!populateEmpty)
            {
                definition.Add(new ResourcePool { Details = new ResourcePoolComputeDetails { Location = DefaultLocation, SkuName = DefaultResourceSkuName }, Type = DefaultType, TargetCount = 10, LogicalSkus = new List<string> { DefaultLogicalSkuName } });
                definition.Add(new ResourcePool { Details = new ResourcePoolStorageDetails { Location = DefaultLocation, SkuName = StorageResourceSkuName }, Type = ResourceType.StorageFileShare, TargetCount = 10, LogicalSkus = new List<string> { DefaultLogicalSkuName } });
                definition.Add(new ResourcePool { Details = new ResourcePoolComputeDetails { Location = WestLocation, SkuName = DefaultResourceSkuName }, Type = DefaultType, TargetCount = 10, LogicalSkus = new List<string> { DefaultLogicalSkuName } });

                DefaultPoolCode = definition[0].Details.GetPoolDefinition();
            }
            else
            {
                DefaultPoolCode = "PoolCode";
            }

            var resourceScalingStore = new Mock<IResourcePoolDefinitionStore>();
            resourceScalingStore
                .Setup(x => x.RetrieveDefinitionsAsync())
                .Returns(Task.FromResult((IList<ResourcePool>)definition));

            return resourceScalingStore;
        }

        private Mock<IDiagnosticsLogger> BuildLogger()
        {
            var logger = new Mock<IDiagnosticsLogger>();
            logger.Setup(x => x.WithValues(It.IsAny<LogValueSet>())).Returns(logger.Object);
            return logger;
        }

        private AllocateInput BuildAllocateInput()
        {
            return new AllocateInput
            {
                Location = DefaultLocation,
                SkuName = DefaultLogicalSkuName,
                Type = DefaultType
            };
        }

        private StartInput BuildStartInput(Guid resourceId)
        {
            return new StartInput
            {
                ResourceId = resourceId,
            };
        }

        private SuspendInput BuildSuspendInput(Guid resourceId)
        {
            return new SuspendInput
            {
                ResourceId = resourceId,
            };
        }

        private DeleteInput BuildDeleteInput(Guid resourceId)
        {
            return new DeleteInput
            {
                ResourceId = resourceId,
            };
        }

        private StatusInput BuildStatusInput(Guid resourceId)
        {
            return new StatusInput
            {
                ResourceId = resourceId,
            };
        }

        private ResourceRecord BuildResourceRecord()
        {
            return new ResourceRecord
            {
                SkuName = DefaultResourceSkuName,
                Type = DefaultType,
                Created = DateTime.UtcNow,
                IsAssigned = false,
                Assigned = DateTime.UtcNow
            };
        }

        private IMapper BuildMapper()
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ResourceRecord, AllocateResult>();
            });
            configuration.AssertConfigurationIsValid();
            return configuration.CreateMapper();
        }
    }
}
