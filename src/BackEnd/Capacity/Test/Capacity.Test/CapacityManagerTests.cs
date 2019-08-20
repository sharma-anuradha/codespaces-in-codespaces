using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Test
{
    public class CapacityManagerTests
    {
        [Fact]
        public void Cotr_Throws_NullArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => new CapacityManager(null));
        }

        [Fact]
        public async Task SelectAzureResourceLocation_Throws_NullArgumentException()
        {
            var catalog = MockAzureSubscriptionCatalog();
            var capacityManager = new CapacityManager(catalog);
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
            var sku = new Mock<ICloudEnvironmentSku>().Object;
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var capacityManager = new CapacityManager(catalog);

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
            var sku = new Mock<ICloudEnvironmentSku>().Object;
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var capacityManager = new CapacityManager(catalog);

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
    }
}
