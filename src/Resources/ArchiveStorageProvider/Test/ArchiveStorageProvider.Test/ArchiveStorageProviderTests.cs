using System;
using Microsoft.VsSaaS.Azure.Metrics;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Test
{
    public class ArchiveStorageProviderTests
    {
        [Fact]
        public void ArchiveStorageProvider_Ctor_Throws_ArgumentNullExceptions()
        {
            var capacityManager = new Mock<ICapacityManager>().Object;
            var controlPlaneInfo = new Mock<IControlPlaneInfo>().Object;
            var subscriptionCataolog = new Mock<IAzureSubscriptionCatalog>().Object;
            var azureClientFactory = new Mock<IAzureClientFactory>().Object;
            var metricsProvider = new Mock<IMetricsProvider>().Object;
            var resourceNameBuilder = new Mock<IResourceNameBuilder>().Object;
            var personalStampSettings = new DeveloperPersonalStampSettings(false, null, false);

            var defaultLogValues = new Mock<LogValueSet>().Object;
            var diagnosticsLoggerFactory = new Mock<IDiagnosticsLoggerFactory>().Object;

            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(null, controlPlaneInfo, subscriptionCataolog, azureClientFactory, metricsProvider, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, null, subscriptionCataolog, azureClientFactory, metricsProvider, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, controlPlaneInfo, null, azureClientFactory, metricsProvider, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, controlPlaneInfo, subscriptionCataolog, null, metricsProvider, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, controlPlaneInfo, subscriptionCataolog, azureClientFactory, null, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, controlPlaneInfo, subscriptionCataolog, azureClientFactory, metricsProvider, null, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, controlPlaneInfo, subscriptionCataolog, azureClientFactory, metricsProvider, resourceNameBuilder, null, diagnosticsLoggerFactory, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, controlPlaneInfo, subscriptionCataolog, azureClientFactory, metricsProvider, resourceNameBuilder, personalStampSettings, null, defaultLogValues));
            Assert.Throws<ArgumentNullException>(() => new ArchiveStorageProvider(capacityManager, controlPlaneInfo, subscriptionCataolog, azureClientFactory, metricsProvider, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, null));
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
            var subscriptionCatalog = new Mock<IAzureSubscriptionCatalog>().Object;
            var azureClientFactory = new Mock<IAzureClientFactory>().Object;
            var metricsProvider = new Mock<IMetricsProvider>().Object;
            var resourceNameBuilder = new Mock<IResourceNameBuilder>().Object;
            var personalStampSettings = new DeveloperPersonalStampSettings(false, null, false);
            var diagnosticsLoggerFactory = new Mock<IDiagnosticsLoggerFactory>().Object;
            var defaultLogValues = new Mock<LogValueSet>().Object;

            return new ArchiveStorageProvider(capacityManager, controlPlaneInfo, subscriptionCatalog, azureClientFactory, metricsProvider, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues);
        }
    }
}
