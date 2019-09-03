// <copyright file="UnitTest1.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
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
        private const string DefaultResourceSkuName = "LargeVm";
        private const ResourceType DefaultType = ResourceType.ComputeVM;

        [Fact]
        public void Ctor_throws_if_null()
        {
            var resourceRepository = new Mock<IResourceRepository>().Object;

            Assert.Throws<ArgumentNullException>(() => new ResourcePool(null));
        }

        [Fact]
        public void Ctor_ok()
        {
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var pool = new ResourcePool(resourceRepository);
            Assert.NotNull(pool);
        }

        [Fact()]
        public async void ResourcePool_WhenHasCapacity_ReturnsResource()
        {
            var rawResult = BuildResourceRecord();
            var input = BuildAllocateInput();

            var logger = new Mock<IDiagnosticsLogger>();
            logger.Setup(l => l.WithValue(It.IsAny<string>(), It.IsAny<string>())).Returns(logger.Object);
            var repository = new Mock<IResourceRepository>();
            repository.Setup(x => x.GetUnassignedResourceAsync(DefaultResourceSkuName, DefaultType, DefaultLocation, logger.Object))
                .Returns(Task.FromResult(rawResult));

            var provider = new ResourcePool(repository.Object);

            var result = await provider.TryGetAsync(input.SkuName, input.Type, input.Location, logger.Object);

            Assert.NotNull(result);
            Assert.True(result.IsAssigned);
            Assert.Equal(rawResult, result);
        }

        [Fact()]
        public async void ResourceBroker_WhenHasNoCapacity_ReturnsNull()
        {
            var input = BuildAllocateInput();

            var logger = new Mock<IDiagnosticsLogger>();
            logger.Setup(l => l.WithValue(It.IsAny<string>(), It.IsAny<string>())).Returns(logger.Object);
            var repository = new Mock<IResourceRepository>();
            repository.Setup(x => x.GetUnassignedResourceAsync(DefaultResourceSkuName, DefaultType, DefaultLocation, It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((ResourceRecord)null));

            var provider = new ResourcePool(repository.Object);

            var result = await provider.TryGetAsync(input.SkuName, input.Type, input.Location, logger.Object);

            Assert.Null(result);
        }

        [Fact()]
        public async void ResourcePool_WhenHasDeadlockCanStillFindCapacity_ReturnsResource()
        {
            var rawResult1 = BuildResourceRecord();
            var rawResult2 = BuildResourceRecord();
            var rawResult3 = BuildResourceRecord();
            var input = BuildAllocateInput();

            var exception = CreateDocumentClientException(new Error(), HttpStatusCode.PreconditionFailed);

            var mockLogger = new Mock<IDiagnosticsLogger>();
            var logger = mockLogger.Object;
            mockLogger.Setup(l => l.WithValue(It.IsAny<string>(), It.IsAny<string>())).Returns(logger);
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

            var provider = new ResourcePool(repository.Object);

            var result = await provider.TryGetAsync(input.SkuName, input.Type, input.Location, logger);

            Assert.NotNull(result);
            Assert.True(result.IsAssigned);
            Assert.Equal(rawResult3, result);
        }

        [Fact()]
        public async void ResourcePool_WhenHasDeadlockAndRetriesExhausted_ReturnsResource()
        {
            var rawResult1 = BuildResourceRecord();
            var rawResult2 = BuildResourceRecord();
            var rawResult3 = BuildResourceRecord();
            var input = BuildAllocateInput();

            var exception = CreateDocumentClientException(new Error(), HttpStatusCode.PreconditionFailed);

            var mockLogger = new Mock<IDiagnosticsLogger>();
            var logger = mockLogger.Object;
            mockLogger.Setup(l => l.WithValue(It.IsAny<string>(), It.IsAny<string>())).Returns(logger);
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

            var provider = new ResourcePool(repository.Object);

            var result = await provider.TryGetAsync(input.SkuName, input.Type, input.Location, logger);

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


        private AllocateInput BuildAllocateInput()
        {
            return new AllocateInput
            {
                Location = DefaultLocation,
                SkuName = DefaultResourceSkuName,
                Type = DefaultType
            };
        }

        private ResourceRecord BuildResourceRecord()
        {
            return new ResourceRecord
            {
                SkuName = DefaultResourceSkuName,
                Type = DefaultType,
                AzureResourceInfo = null,
                Created = DateTime.UtcNow,
                IsAssigned = false,
                Assigned = DateTime.UtcNow
            };
        }
    }
}
