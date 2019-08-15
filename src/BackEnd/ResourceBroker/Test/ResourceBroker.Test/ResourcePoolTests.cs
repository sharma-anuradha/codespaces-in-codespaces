// <copyright file="UnitTest1.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Azure.Documents;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Test
{
    public class ResourcePoolTests
    {
        private const string DefaultLocation = "USW2";
        private const string WestLocation = "USW2";
        private const string DefaultLogicalSkuName = "Large";
        private const string DefaultResourceSkuName = "LargeVm";
        private const string StorageResourceSkuName = "LargeVm";
        private const ResourceType DefaultType = ResourceType.ComputeVM;

        [Fact]
        public void Ctor_throws_if_null()
        {
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourceScalingStore = new Mock<IResourceScalingStore>().Object;

            Assert.Throws<ArgumentNullException>(() => new ResourcePool(null, null));
            Assert.Throws<ArgumentNullException>(() => new ResourcePool(resourceRepository, null));
            Assert.Throws<ArgumentNullException>(() => new ResourcePool(null, resourceScalingStore));
        }

        [Fact]
        public void Ctor_ok()
        {
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var resourceScalingStore = new Mock<IResourceScalingStore>().Object;
            var pool = new ResourcePool(resourceRepository, resourceScalingStore);
            Assert.NotNull(pool);
        }

        [Fact]
        public async void ResourcePool_WhenHasCapacity_ReturnsResource()
        {
            var rawResult = BuildResourceRecord();
            var input = BuildAllocateInput();

            var scalingStore = BuildResourceScalingStore();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var repository = new Mock<IResourceRepository>();
            repository.Setup(x => x.GetUnassignedResourceAsync(DefaultResourceSkuName, DefaultType, DefaultLocation, logger))
                .Returns(Task.FromResult(rawResult));

            var provider = new ResourcePool(repository.Object, scalingStore.Object);

            var result = await provider.TryGetAsync(input, logger);

            Assert.NotNull(result);
            Assert.True(result.IsAssigned);
            Assert.Equal(rawResult, result);
        }

        [Fact]
        public async void ResourceBroker_WhenHasNoCapacity_ReturnsNull()
        {
            var input = BuildAllocateInput();

            var scalingStore = BuildResourceScalingStore();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var repository = new Mock<IResourceRepository>();
            repository.Setup(x => x.GetUnassignedResourceAsync(DefaultResourceSkuName, DefaultType, DefaultLocation, logger))
                .Returns(Task.FromResult((ResourceRecord)null));

            var provider = new ResourcePool(repository.Object, scalingStore.Object);

            var result = await provider.TryGetAsync(input, logger);

            Assert.Null(result);
        }

        [Fact]
        public async void ResourcePool_WhenHasDeadlockCanStillFindCapacity_ReturnsResource()
        {
            var rawResult1 = BuildResourceRecord();
            var rawResult2 = BuildResourceRecord();
            var rawResult3 = BuildResourceRecord();
            var input = BuildAllocateInput();

            var exception = CreateDocumentClientException(new Error(), HttpStatusCode.PreconditionFailed);

            var scalingStore = BuildResourceScalingStore();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var repository = new Mock<IResourceRepository>();
            repository
                .SetupSequence(x => x.GetUnassignedResourceAsync(DefaultResourceSkuName, DefaultType, DefaultLocation, logger))
                .Returns(Task.FromResult(rawResult1))
                .Returns(Task.FromResult(rawResult2))
                .Returns(Task.FromResult(rawResult3));
            repository
                .Setup(x => x.UpdateAsync(rawResult1, logger))
                .Throws(exception);
            repository
                .Setup(x => x.UpdateAsync(rawResult2, logger))
                .Throws(exception);
            repository
                .Setup(x => x.UpdateAsync(rawResult3, logger))
                .Returns(Task.FromResult(rawResult3));

            var provider = new ResourcePool(repository.Object, scalingStore.Object);

            var result = await provider.TryGetAsync(input, logger);

            Assert.NotNull(result);
            Assert.True(result.IsAssigned);
            Assert.Equal(rawResult3, result);
        }

        [Fact]
        public async void ResourcePool_WhenHasDeadlockAndRetriesExhausted_ReturnsResource()
        {
            var rawResult1 = BuildResourceRecord();
            var rawResult2 = BuildResourceRecord();
            var rawResult3 = BuildResourceRecord();
            var input = BuildAllocateInput();

            var exception = CreateDocumentClientException(new Error(), HttpStatusCode.PreconditionFailed);

            var scalingStore = BuildResourceScalingStore();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var repository = new Mock<IResourceRepository>();
            repository
                .SetupSequence(x => x.GetUnassignedResourceAsync(DefaultResourceSkuName, DefaultType, DefaultLocation, logger))
                .Returns(Task.FromResult(rawResult1))
                .Returns(Task.FromResult(rawResult2))
                .Returns(Task.FromResult(rawResult3));
            repository
                .Setup(x => x.UpdateAsync(rawResult1, logger))
                .Throws(exception);
            repository
                .Setup(x => x.UpdateAsync(rawResult2, logger))
                .Throws(exception);
            repository
                .Setup(x => x.UpdateAsync(rawResult3, logger))
                .Throws(exception);

            var provider = new ResourcePool(repository.Object, scalingStore.Object);

            var result = await provider.TryGetAsync(input, logger);

            Assert.Null(result);
        }

        private static DocumentClientException CreateDocumentClientException(
            Error error,
            HttpStatusCode httpStatusCode)
        {
            var type = typeof(DocumentClientException);

            var documentClientExceptionInstance = type.Assembly.CreateInstance(type.FullName,
                false, BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { error, (HttpResponseHeaders)null, httpStatusCode }, null, null);

            return (DocumentClientException)documentClientExceptionInstance;
        }

        private Mock<IResourceScalingStore> BuildResourceScalingStore()
        {
            var definition = new List<ResourcePoolDefinition>()
            {
                new ResourcePoolDefinition { Location = DefaultLocation, SkuName = DefaultResourceSkuName, TargetCount = 10, Type = DefaultType, EnvironmentSkus = new List<string> { DefaultLogicalSkuName } },
                new ResourcePoolDefinition { Location = DefaultLocation, SkuName = StorageResourceSkuName, TargetCount = 10, Type = ResourceType.StorageFileShare, EnvironmentSkus = new List<string> { DefaultLogicalSkuName } },
                new ResourcePoolDefinition { Location = WestLocation, SkuName = DefaultResourceSkuName, TargetCount = 10, Type = DefaultType, EnvironmentSkus = new List<string> { DefaultLogicalSkuName } }
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
    }
}
