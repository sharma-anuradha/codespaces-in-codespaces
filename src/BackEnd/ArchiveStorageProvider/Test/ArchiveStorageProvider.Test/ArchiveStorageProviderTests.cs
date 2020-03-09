using System;
using Microsoft.VsSaaS.Azure.Metrics;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider.Test
{
    public class ArchiveStorageProviderTests
    {
        [Fact]
        public void ArchiveStorageProvider_Ctor_Throws_ArgumentNullExceptions()
        {
            var capacityManager = new Mock<ICapacityManager>().Object;
            var controlPlaneInfo = new Mock<IControlPlaneInfo>().Object;
            var systemCatalog = new Mock<ISystemCatalog>().Object;
            var metricsProvider = new Mock<IMetricsProvider>().Object;
            var resourceNameBuilder = new Mock<IResourceNameBuilder>().Object;
            var personalStampSettings = new DeveloperPersonalStampSettings(false, null);

            var defaultLogValues = new Mock<LogValueSet>().Object;
            var diagnosticsLoggerFactory = new Mock<IDiagnosticsLoggerFactory>().Object;

            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(null, controlPlaneInfo, systemCatalog, metricsProvider, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, null, systemCatalog, metricsProvider, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, controlPlaneInfo, null, metricsProvider, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, controlPlaneInfo, systemCatalog, null, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, controlPlaneInfo, systemCatalog, metricsProvider, null, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, controlPlaneInfo, systemCatalog, metricsProvider, resourceNameBuilder, null, diagnosticsLoggerFactory, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, controlPlaneInfo, systemCatalog, metricsProvider, resourceNameBuilder, personalStampSettings, null, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, controlPlaneInfo, systemCatalog, metricsProvider, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, null));
        }

        [Fact]
        public void ArchiveStorageProvider_Ctor_OK()
        {
            var asp = CreateMockArchiveStorageProvider();
            Assert.NotNull(asp);
        }

        // TODO: further testing requires much deeper mocks due to the direct use of IAzure clients.

        private IArchiveStorageProvider CreateMockArchiveStorageProvider()
        {
            var capacityManager = new Mock<ICapacityManager>().Object;
            var controlPlaneInfo = new Mock<IControlPlaneInfo>().Object;
            var systemCatalog = new Mock<ISystemCatalog>().Object;
            var metricsProvider = new Mock<IMetricsProvider>().Object;
            var resourceNameBuilder = new Mock<IResourceNameBuilder>().Object;
            var personalStampSettings = new DeveloperPersonalStampSettings(false, null);
            var diagnosticsLoggerFactory = new Mock<IDiagnosticsLoggerFactory>().Object;
            var defaultLogValues = new Mock<LogValueSet>().Object;

            return new ArchiveStorageProvider(capacityManager, controlPlaneInfo, systemCatalog, metricsProvider, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues);
        }
    }
}
