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
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Test
{
    public class ResourcePoolManagerTests
    {
        private const string DefaultPoolCode = "123ABC";

        [Fact]
        public void Ctor_throws_if_null()
        {
            var resourceRepository = new Mock<IResourceRepository>().Object;

            Assert.Throws<ArgumentNullException>(() => new ResourcePoolManager(null));
        }

        [Fact]
        public void Ctor_ok()
        {
            var resourceRepository = new Mock<IResourceRepository>().Object;
            var pool = new ResourcePoolManager(resourceRepository);
            Assert.NotNull(pool);
        }

        [Fact()]
        public async void ResourcePoolManager_WhenHasCapacity_ReturnsResource()
        {
            var rawResult = BuildResourceRecord();

            var logger = new Mock<IDiagnosticsLogger>();
            logger.Setup(l => l.WithValues(It.IsAny<LogValueSet>())).Returns(logger.Object);
            var repository = new Mock<IResourceRepository>();
            repository.Setup(x => x.GetPoolReadyUnassignedAsync(DefaultPoolCode, logger.Object))
                .Returns(Task.FromResult(rawResult));

            var provider = new ResourcePoolManager(repository.Object);

            var result = await provider.TryGetAsync(DefaultPoolCode, logger.Object);

            Assert.NotNull(result);
            Assert.True(result.IsAssigned);
            Assert.Equal(rawResult, result);
        }

        [Fact()]
        public async void ResourceBroker_WhenHasNoCapacity_ReturnsNull()
        {
            var logger = new Mock<IDiagnosticsLogger>();
            logger.Setup(l => l.WithValues(It.IsAny<LogValueSet>())).Returns(logger.Object);
            var repository = new Mock<IResourceRepository>();
            repository.Setup(x => x.GetPoolReadyUnassignedAsync(DefaultPoolCode, It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((ResourceRecord)null));

            var provider = new ResourcePoolManager(repository.Object);

            var result = await provider.TryGetAsync(DefaultPoolCode, logger.Object);

            Assert.Null(result);
        }

        [Fact()]
        public async void ResourcePoolManager_WhenHasDeadlockCanStillFindCapacity_ReturnsResource()
        {
            var rawResult1 = BuildResourceRecord();
            var rawResult2 = BuildResourceRecord();
            var rawResult3 = BuildResourceRecord();

            var exception = CreateDocumentClientException(new Error(), HttpStatusCode.PreconditionFailed);

            var mockLogger = new Mock<IDiagnosticsLogger>();
            var logger = mockLogger.Object;
            mockLogger.Setup(l => l.WithValues(It.IsAny<LogValueSet>())).Returns(logger);
            var repository = new Mock<IResourceRepository>();
            repository
                .SetupSequence(x => x.GetPoolReadyUnassignedAsync(DefaultPoolCode, logger))
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

            var provider = new ResourcePoolManager(repository.Object);

            var result = await provider.TryGetAsync(DefaultPoolCode, logger);

            Assert.NotNull(result);
            Assert.True(result.IsAssigned);
            Assert.Equal(rawResult3, result);
        }

        [Fact()]
        public async void ResourcePoolManager_WhenHasDeadlockAndRetriesExhausted_ReturnsResource()
        {
            var rawResult1 = BuildResourceRecord();
            var rawResult2 = BuildResourceRecord();
            var rawResult3 = BuildResourceRecord();

            var exception = CreateDocumentClientException(new Error(), HttpStatusCode.PreconditionFailed);

            var mockLogger = new Mock<IDiagnosticsLogger>();
            var logger = mockLogger.Object;
            mockLogger.Setup(l => l.WithValues(It.IsAny<LogValueSet>())).Returns(logger);
            var repository = new Mock<IResourceRepository>();
            repository
                .SetupSequence(x => x.GetPoolReadyUnassignedAsync(DefaultPoolCode, logger))
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

            var provider = new ResourcePoolManager(repository.Object);

            var result = await provider.TryGetAsync(DefaultPoolCode, logger);

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

        private ResourceRecord BuildResourceRecord()
        {
            return new ResourceRecord();
        }
    }
}
