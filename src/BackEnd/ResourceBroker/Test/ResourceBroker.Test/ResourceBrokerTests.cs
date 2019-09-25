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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Test
{
    public class ResourceBrokerTests
    {
        private string DefaultPoolCode = "PoolCode";
        private const AzureLocation DefaultLocation = AzureLocation.EastUs;
        private const AzureLocation WestLocation = AzureLocation.WestUs2;
        private const string DefaultResourceSkuName = "LargeVm";
        private const string DefaultLogicalSkuName = "Large";
        private const string StorageResourceSkuName = "LargeVm";
        private const ResourceType DefaultType = ResourceType.ComputeVM;
        private const ResourceType StorageType = ResourceType.StorageFileShare;

        /*
        private const string DefaultLocation = "USW2";
         */

        [Fact]
        public void Ctor_throws_if_null()
        {
            var resourcePool = new Mock<IResourcePoolManager>().Object;
            var resourceScalingStore = new Mock<IResourcePoolDefinitionStore>().Object;
            var continuationTaskActivator = new Mock<IContinuationTaskActivator>().Object;
            var taskHelper = new Mock<ITaskHelper>().Object;
            var mapper = new Mock<IMapper>().Object;

            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, null, null, null, mapper));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, null, null, taskHelper, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, null, continuationTaskActivator, null, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, resourceScalingStore, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(resourcePool, null, null, null, null));
        }

        [Fact]
        public void Ctor_ok()
        {
            var resourcePool = new Mock<IResourcePoolManager>().Object;
            var resourceScalingStore = new Mock<IResourcePoolDefinitionStore>().Object;
            var continuationTaskActivator = new Mock<IContinuationTaskActivator>().Object;
            var taskHelper = new Mock<ITaskHelper>().Object;
            var mapper = new Mock<IMapper>().Object;
            var provider = new ResourceBroker(resourcePool, resourceScalingStore, continuationTaskActivator, taskHelper, mapper);
            Assert.NotNull(provider);
        }

        [Fact()]
        public async void ResourceBroker_WhenHasCapacity_ReturnsResource()
        {
            var input = BuildAllocateInput();
            var rawResult = BuildResourceRecord();

            var scalingStore = BuildResourceScalingStore();
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var continuationTaskActivator = new Mock<IContinuationTaskActivator>().Object;
            var resourcePool = new Mock<IResourcePoolManager>();
            resourcePool.Setup(x => x.TryGetAsync(DefaultPoolCode, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(rawResult));

            var provider = new ResourceBroker(resourcePool.Object, scalingStore.Object, continuationTaskActivator, taskHelper, mapper);

            var result = await provider.AllocateAsync(input, logger);

            Assert.NotNull(result);
            Assert.Equal(rawResult.Created, result.Created);
            Assert.Equal(rawResult.SkuName, result.SkuName);
            Assert.Equal(rawResult.Type, result.Type);
        }

        [Fact]
        public void ResourceBroker_WhenHasNoCapacity_ReturnsNull()
        {
            var input = BuildAllocateInput();

            var scalingStore = BuildResourceScalingStore();
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var continuationTaskActivator = new Mock<IContinuationTaskActivator>().Object;
            var resourcePool = new Mock<IResourcePoolManager>();
            resourcePool.Setup(x => x.TryGetAsync(DefaultPoolCode, logger)).Returns(Task.FromResult((ResourceRecord)null));

            var provider = new ResourceBroker(resourcePool.Object, scalingStore.Object, continuationTaskActivator, taskHelper, mapper);

            Assert.ThrowsAsync<OutOfCapacityException>(async () => await provider.AllocateAsync(input, logger));
        }

        [Fact]
        public void ResourceBroker_WhenNoLogicalToResourceSkuMatchOccurs_ThrowsException()
        {
            var input = BuildAllocateInput();

            var scalingStore = BuildResourceScalingStore(true);
            var mapper = BuildMapper();
            var taskHelper = new Mock<ITaskHelper>().Object;
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var continuationTaskActivator = new Mock<IContinuationTaskActivator>().Object;
            var resourcePool = new Mock<IResourcePoolManager>();
            resourcePool.Setup(x => x.TryGetAsync(DefaultPoolCode, logger)).Returns(Task.FromResult((ResourceRecord)null));
            
            var provider = new ResourceBroker(resourcePool.Object, scalingStore.Object, continuationTaskActivator, taskHelper, mapper);

            Assert.ThrowsAsync<ArgumentException>(async () => await provider.AllocateAsync(input, logger));
        }

        private Mock<IResourcePoolDefinitionStore> BuildResourceScalingStore(bool populateEmpty = false)
        {
            var definition = new List<ResourcePool>();
            if (!populateEmpty)
            {
                definition.Add(new ResourcePool { Details = new ResourcePoolComputeDetails { Location = DefaultLocation, SkuName = DefaultResourceSkuName }, Type = DefaultType, TargetCount = 10, EnvironmentSkus = new List<string> { DefaultLogicalSkuName } });
                definition.Add(new ResourcePool { Details = new ResourcePoolStorageDetails { Location = DefaultLocation, SkuName = StorageResourceSkuName }, Type = ResourceType.StorageFileShare, TargetCount = 10, EnvironmentSkus = new List<string> { DefaultLogicalSkuName } });
                definition.Add(new ResourcePool { Details = new ResourcePoolComputeDetails { Location = WestLocation, SkuName = DefaultResourceSkuName }, Type = DefaultType, TargetCount = 10, EnvironmentSkus = new List<string> { DefaultLogicalSkuName } });

                DefaultPoolCode = definition[0].Details.GetPoolDefinition();
            }
            else
            {
                DefaultPoolCode = "PoolCode";
            }

            var resourceScalingStore = new Mock<IResourcePoolDefinitionStore>();
            resourceScalingStore
                .Setup(x => x.RetrieveDefinitions())
                .Returns(Task.FromResult((IEnumerable<ResourcePool>)definition));

            return resourceScalingStore;
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
