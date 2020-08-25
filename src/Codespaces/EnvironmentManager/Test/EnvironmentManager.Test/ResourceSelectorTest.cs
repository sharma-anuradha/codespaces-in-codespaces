using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Test
{
    public class ResourceSelectorTest
    {
        private readonly IDiagnosticsLogger logger = new Mock<IDiagnosticsLogger>().Object;

        private delegate void TempDelegate(string key, out ICloudEnvironmentSku outVar);

        [Fact]
        public void ResourceSelector_ctor_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ResourceSelectorFactory(null, null));
            Assert.Throws<ArgumentNullException>(() => new ResourceSelectorFactory(null, new Mock<ISystemConfiguration>().Object));
            Assert.Throws<ArgumentNullException>(() => new ResourceSelectorFactory(new Mock<ISkuCatalog>().Object, null));
        }

        [Fact]
        public async Task ResourceSelector_Allocate_Windows_With_WindowsOSDiskFeatureAsync()
        {
            var (skuCatalog, systemConfiguration) = GetParameters(true, ComputeOS.Windows);

            var resourceSelector = new ResourceSelectorFactory(skuCatalog, systemConfiguration);

            var cloudEnvironment = new CloudEnvironment();

            var requests = await resourceSelector.CreateAllocationRequestsAsync(cloudEnvironment, logger);

            Assert.True(requests.Count == 2);
            Assert.NotNull(requests.Single(x => x.Type == ResourceType.ComputeVM));
            Assert.NotNull(requests.Single(x => x.Type == ResourceType.OSDisk));
        }

        [Fact]
        public async Task ResourceSelector_Allocate_Linux_With_WindowsOSDiskFeatureAsync()
        {
            var (skuCatalog, systemConfiguration) = GetParameters(true, ComputeOS.Linux);

            var resourceSelector = new ResourceSelectorFactory(skuCatalog, systemConfiguration);

            var cloudEnvironment = new CloudEnvironment();

            var requests = await resourceSelector.CreateAllocationRequestsAsync(cloudEnvironment, logger);

            Assert.True(requests.Count == 2);
            Assert.NotNull(requests.Single(x => x.Type == ResourceType.ComputeVM));
            Assert.NotNull(requests.Single(x => x.Type == ResourceType.StorageFileShare));
        }

        [Fact]
        public async Task ResourceSelector_Allocate_Linux_With_OSDiskFeatureDisabledAsync()
        {
            var (skuCatalog, systemConfiguration) = GetParameters(false, ComputeOS.Linux);

            var resourceSelector = new ResourceSelectorFactory(skuCatalog, systemConfiguration);

            var cloudEnvironment = new CloudEnvironment();

            var requests = await resourceSelector.CreateAllocationRequestsAsync(cloudEnvironment, logger);

            Assert.True(requests.Count == 2);
            Assert.NotNull(requests.Single(x => x.Type == ResourceType.ComputeVM));
            Assert.NotNull(requests.Single(x => x.Type == ResourceType.StorageFileShare));
        }

        [Fact]
        public async Task ResourceSelector_Allocate_Windows_With_OSDiskFeatureDisabledAsync()
        {
            var (skuCatalog, systemConfiguration) = GetParameters(false, ComputeOS.Windows);

            var resourceSelector = new ResourceSelectorFactory(skuCatalog, systemConfiguration);

            var cloudEnvironment = new CloudEnvironment();

            var requests = await resourceSelector.CreateAllocationRequestsAsync(cloudEnvironment, logger);

            Assert.True(requests.Count == 2);
            Assert.NotNull(requests.Single(x => x.Type == ResourceType.ComputeVM));
            Assert.NotNull(requests.Single(x => x.Type == ResourceType.StorageFileShare));
        }

        private (ISkuCatalog, ISystemConfiguration) GetParameters(bool enableOSDiskFeature, ComputeOS computeOS)
        {
            var systemConfiguration = new Mock<ISystemConfiguration>();
            systemConfiguration
                .Setup(x => x.GetValueAsync<bool>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(enableOSDiskFeature));
            var skuCatalog = new Mock<ISkuCatalog>();
            skuCatalog.Setup(x => x.CloudEnvironmentSkus).Returns(GetSkuCatalog(computeOS));

            return (skuCatalog.Object, systemConfiguration.Object);
        }

        private static IReadOnlyDictionary<string, ICloudEnvironmentSku> GetSkuCatalog(ComputeOS computeOS)
        {
            var testSku = new Mock<ICloudEnvironmentSku>();
            testSku.Setup(x => x.ComputeOS).Returns(computeOS);

            var testSkuCatalog = new Mock<IReadOnlyDictionary<string, ICloudEnvironmentSku>>();
            ICloudEnvironmentSku d;
            testSkuCatalog
                .Setup(x => x.TryGetValue(It.IsAny<string>(), out d))
                .Callback(new TempDelegate((string key, out ICloudEnvironmentSku v) => { v = testSku.Object; }))
                .Returns(true);

            return testSkuCatalog.Object;
        }
    }
}
