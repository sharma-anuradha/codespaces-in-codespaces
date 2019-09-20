using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Test
{
    public class CapacityManagerTests
    {
        [Fact]
        public void Ctor_Throws_NullArgumentException()
        {
            var azureSubscriptionCatalog = new Mock<IAzureSubscriptionCatalog>().Object;
            var controlPlaneInfo = new Mock<IControlPlaneInfo>().Object;
            var capacitySettings = new CapacitySettings();
            var resourceNameBuilder = new ResourceNameBuilder(new DeveloperPersonalStampSettings(false));
            _ = new CapacityManager(azureSubscriptionCatalog, controlPlaneInfo, resourceNameBuilder, capacitySettings);
            Assert.Throws<ArgumentNullException>(() => new CapacityManager(null, controlPlaneInfo, resourceNameBuilder, capacitySettings));
            Assert.Throws<ArgumentNullException>(() => new CapacityManager(null, null, resourceNameBuilder, capacitySettings));
            Assert.Throws<ArgumentNullException>(() => new CapacityManager(null, null, null, capacitySettings));
            Assert.Throws<ArgumentNullException>(() => new CapacityManager(null, null, null, null));
        }

        [Fact(Skip = "Skipping null arg test until resource broker can invoke SelectAzureResourceLocation with non-null.")]
        public async Task SelectAzureResourceLocation_Throws_NullArgumentException()
        {
            var catalog = MockAzureSubscriptionCatalog();
            var controlPlaneResourceAccessor = MockControlPlaneInfo();
            var capacitySettings = new CapacitySettings();
            var resourceNameBuilder = new ResourceNameBuilder(new DeveloperPersonalStampSettings(false));


            var capacityManager = new CapacityManager(catalog, controlPlaneResourceAccessor, resourceNameBuilder, capacitySettings);
            var sku = new Mock<ICloudEnvironmentSku>().Object;
            var logger = new Mock<IDiagnosticsLogger>().Object;

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                capacityManager.SelectAzureResourceLocation(null, AzureLocation.EastUs, logger));

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                capacityManager.SelectAzureResourceLocation(sku, AzureLocation.EastUs, null));
        }

        [Fact]
        public async Task SelectAzureResourceLocation_Throws_SkuNotAvailableException()
        {
            var catalog = MockAzureSubscriptionCatalog();
            var controlPlaneResourceAccessor = MockControlPlaneInfo();
            var sku = new Mock<ICloudEnvironmentSku>().Object;
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var resourceNameBuilder = new ResourceNameBuilder(new DeveloperPersonalStampSettings(false));
            var capacitySettings = new CapacitySettings();

            var capacityManager = new CapacityManager(catalog, controlPlaneResourceAccessor, resourceNameBuilder, capacitySettings);

            var badLocation = AzureLocation.EastUs2Euap;
            await Assert.ThrowsAsync<SkuNotAvailableException>(() =>
                capacityManager.SelectAzureResourceLocation(sku, badLocation, logger));
        }

        public static readonly TheoryData<AzureLocation> GoodLocations = new TheoryData<AzureLocation>()
        {
            AzureLocation.EastUs,
            AzureLocation.SouthEastAsia,
            AzureLocation.WestEurope,
            AzureLocation.WestUs2,
        };

        [Theory]
        [MemberData(nameof(GoodLocations))]
        public async Task SelectAzureResourceLocation_OK(AzureLocation location)
        {
            var catalog = MockAzureSubscriptionCatalog();
            var controlPlaneInfo = MockControlPlaneInfo();
            var sku = new Mock<ICloudEnvironmentSku>().Object;
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var capacitySettings = new CapacitySettings();
            var resourceNameBuilder = new ResourceNameBuilder(new DeveloperPersonalStampSettings(false));

            var capacityManager = new CapacityManager(catalog, controlPlaneInfo, resourceNameBuilder, capacitySettings);

            var result = await capacityManager.SelectAzureResourceLocation(sku, location, logger);
            Assert.Equal(MockSubscriptionId.ToString(), result.Subscription.SubscriptionId);
        }

        private static Guid MockSubscriptionId = Guid.Parse("a44503f4-f352-4d91-a735-2a955a25a5ff");

        private static IAzureSubscriptionCatalog MockAzureSubscriptionCatalog()
        {
            var mockAzureSubscriptionCatalog = new Mock<IAzureSubscriptionCatalog>();
            mockAzureSubscriptionCatalog.Setup(it => it.AzureSubscriptions).Returns(new AzureSubscription[] 
            {
                new AzureSubscription(
                    MockSubscriptionId.ToString(),
                    "mock-subscription",
                    new Mock<IServicePrincipal>().Object,
                    true,
                    new ReadOnlyCollection<AzureLocation>(
                        new AzureLocation[]
                        {
                            AzureLocation.EastUs,
                            AzureLocation.SouthEastAsia,
                            AzureLocation.WestEurope,
                            AzureLocation.WestUs2,
                        }))
            });

            return mockAzureSubscriptionCatalog.Object;
        }

        private static IControlPlaneInfo MockControlPlaneInfo()
        {
            var controlPlaneInfoMoq = new Mock<IControlPlaneInfo>();
            controlPlaneInfoMoq.Setup(obj => obj.Stamp.StampResourceGroupName).Returns("stamp-resource-group-000");
            return controlPlaneInfoMoq.Object;
        }
    }
}
