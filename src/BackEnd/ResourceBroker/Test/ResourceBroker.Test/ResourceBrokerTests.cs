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

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Test
{
    public class ResourceBrokerTests
    {
        private const string DefaultLocation = "EastUS";
        private const string WestLocation = "WestUS2";
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
            var resourcePool = new Mock<IResourcePool>().Object;
            var resourceScalingStore = new Mock<IResourceScalingStore>().Object;
            var continuationTaskActivator = new Mock<IContinuationTaskActivator>().Object;
            var mapper = new Mock<IMapper>().Object;

            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, null, null, mapper));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, null, continuationTaskActivator, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, resourceScalingStore, null, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(resourcePool, null, null, null));
        }

        [Fact]
        public void Ctor_ok()
        {
            var resourcePool = new Mock<IResourcePool>().Object;
            var resourceScalingStore = new Mock<IResourceScalingStore>().Object;
            var continuationTaskActivator = new Mock<IContinuationTaskActivator>().Object;
            var mapper = new Mock<IMapper>().Object;
            var provider = new ResourceBroker(resourcePool, resourceScalingStore, continuationTaskActivator, mapper);
            Assert.NotNull(provider);
        }

        [Fact]
        public async void ResourceBroker_WhenHasCapacity_ReturnsResource()
        {
            var input = BuildAllocateInput();
            var rawResult = BuildResourceRecord();

            var mapper = BuildMapper();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var continuationTaskActivator = new Mock<IContinuationTaskActivator>().Object;
            var resourcePool = new Mock<IResourcePool>();
            resourcePool.Setup(x => x.TryGetAsync(DefaultResourceSkuName, input.Type, input.Location, logger)).Returns(Task.FromResult(rawResult));
            var scalingStore = BuildResourceScalingStore();

            var provider = new ResourceBroker(resourcePool.Object, scalingStore.Object, continuationTaskActivator, mapper);

            var result = await provider.AllocateAsync(input, logger);

            Assert.NotNull(result);
            Assert.Equal(rawResult.Created, result.Created);
            Assert.Equal(rawResult.Location, result.Location);
            Assert.Equal(rawResult.ResourceId, result.ResourceId);
            Assert.Equal(rawResult.SkuName, result.SkuName);
            Assert.Equal(rawResult.Type, result.Type);
        }

        [Fact]
        public void ResourceBroker_WhenHasNoCapacity_ReturnsNull()
        {
            var input = BuildAllocateInput();

            var mapper = BuildMapper();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var continuationTaskActivator = new Mock<IContinuationTaskActivator>().Object;
            var resourcePool = new Mock<IResourcePool>();
            resourcePool.Setup(x => x.TryGetAsync(DefaultResourceSkuName, input.Type, input.Location, logger)).Returns(Task.FromResult((ResourceRecord)null));
            var scalingStore = BuildResourceScalingStore();

            var provider = new ResourceBroker(resourcePool.Object, scalingStore.Object, continuationTaskActivator, mapper);

            Assert.ThrowsAsync<OutOfCapacityException>(async () => await provider.AllocateAsync(input, logger));
        }

        [Fact]
        public void ResourceBroker_WhenNoLogicalToResourceSkuMatchOccurs_ThrowsException()
        {
            var input = BuildAllocateInput();

            var mapper = BuildMapper();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var continuationTaskActivator = new Mock<IContinuationTaskActivator>().Object;
            var resourcePool = new Mock<IResourcePool>();
            resourcePool.Setup(x => x.TryGetAsync(DefaultResourceSkuName, input.Type, input.Location, logger)).Returns(Task.FromResult((ResourceRecord)null));
            var scalingStore = BuildResourceScalingStore(true);

            var provider = new ResourceBroker(resourcePool.Object, scalingStore.Object, continuationTaskActivator, mapper);

            Assert.ThrowsAsync<ArgumentException>(async () => await provider.AllocateAsync(input, logger));
        }

        private Mock<IResourceScalingStore> BuildResourceScalingStore(bool populateEmpty = false)
        {
            var definition = new List<ResourcePoolDefinition>();
            if (!populateEmpty)
            {
                definition.Add(new ResourcePoolDefinition { Location = DefaultLocation, SkuName = DefaultResourceSkuName, TargetCount = 10, Type = DefaultType, EnvironmentSkus = new List<string> { DefaultLogicalSkuName } });
                definition.Add(new ResourcePoolDefinition { Location = DefaultLocation, SkuName = StorageResourceSkuName, TargetCount = 10, Type = ResourceType.StorageFileShare, EnvironmentSkus = new List<string> { DefaultLogicalSkuName } });
                definition.Add(new ResourcePoolDefinition { Location = WestLocation, SkuName = DefaultResourceSkuName, TargetCount = 10, Type = DefaultType, EnvironmentSkus = new List<string> { DefaultLogicalSkuName } });
            };

            var resourceScalingStore = new Mock<IResourceScalingStore>();
            resourceScalingStore
                .Setup(x => x.RetrieveLatestScaleLevels())
                .Returns(Task.FromResult((IEnumerable<ResourcePoolDefinition>)definition));

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
                ResourceId = "ID",
                SkuName = DefaultResourceSkuName,
                Type = DefaultType,
                Location = DefaultLocation,
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
