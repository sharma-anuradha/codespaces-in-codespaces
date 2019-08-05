// <copyright file="UnitTest1.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using Xunit;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Test
{
    public class ResourceBrokerTests
    {
        private const string DefaultLocation = "USW2";
        private const string DefaultSkuName = "Large";
        private const ResourceType DefaultType = ResourceType.Compute;

        [Fact]
        public void Ctor_throws_if_null()
        {
            var resourcePool = new Mock<IResourcePool>().Object;
            var mapper = new Mock<IMapper>().Object;

            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(null, mapper));
            Assert.Throws<ArgumentNullException>(() => new ResourceBroker(resourcePool, null));
        }

        [Fact]
        public void Ctor_ok()
        {
            var resourcePool = new Mock<IResourcePool>().Object;
            var mapper = new Mock<IMapper>().Object;
            var provider = new ResourceBroker(resourcePool, mapper);
            Assert.NotNull(provider);
        }

        [Fact]
        public async void ResourceBroker_WhenHasCapacity_ReturnsResource()
        {
            var input = BuildAllocateInput();
            var rawResult = BuildResourceRecord();

            var mapper = BuildMapper();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var resourcePool = new Mock<IResourcePool>();
            resourcePool.Setup(x => x.TryGetAsync(input, logger)).Returns(Task.FromResult(rawResult));

            var provider = new ResourceBroker(resourcePool.Object, mapper);

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
            var resourcePool = new Mock<IResourcePool>();
            resourcePool.Setup(x => x.TryGetAsync(input, logger)).Returns(Task.FromResult((ResourceRecord)null));

            var provider = new ResourceBroker(resourcePool.Object, mapper);

            Assert.ThrowsAsync<OutOfCapacityException>(async () => await provider.AllocateAsync(input, logger));
        }

        private AllocateInput BuildAllocateInput()
        {
            return new AllocateInput
            {
                Location = DefaultLocation,
                SkuName = DefaultSkuName,
                Type = DefaultType
            };
        }

        private ResourceRecord BuildResourceRecord()
        {
            return new ResourceRecord
            {
                ResourceId = "ID",
                SkuName = DefaultSkuName,
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
