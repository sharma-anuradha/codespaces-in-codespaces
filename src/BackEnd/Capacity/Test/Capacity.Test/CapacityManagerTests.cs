using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using FluentAssertions.Equivalency;
using Microsoft.Azure.Management.CosmosDB.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
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
            var azureSubscriptionCapacityProvider= new Mock<IAzureSubscriptionCapacityProvider>().Object;
            var controlPlaneInfo = new Mock<IControlPlaneInfo>().Object;
            var capacitySettings = new CapacitySettings();
            var resourceNameBuilder = new ResourceNameBuilder(new DeveloperPersonalStampSettings(false));
            var azureClient = new Mock<IAzureClientFactory>().Object;
            _ = new CapacityManager(azureClient, azureSubscriptionCatalog, azureSubscriptionCapacityProvider, controlPlaneInfo, resourceNameBuilder, capacitySettings);
            Assert.Throws<ArgumentNullException>(() => new CapacityManager(null, azureSubscriptionCatalog, azureSubscriptionCapacityProvider, controlPlaneInfo, resourceNameBuilder, capacitySettings));
            Assert.Throws<ArgumentNullException>(() => new CapacityManager(azureClient, null, azureSubscriptionCapacityProvider, controlPlaneInfo, resourceNameBuilder, capacitySettings));
            Assert.Throws<ArgumentNullException>(() => new CapacityManager(azureClient, azureSubscriptionCatalog, null, controlPlaneInfo, resourceNameBuilder, capacitySettings));
            Assert.Throws<ArgumentNullException>(() => new CapacityManager(azureClient, azureSubscriptionCatalog, azureSubscriptionCapacityProvider, null, resourceNameBuilder, capacitySettings));
            Assert.Throws<ArgumentNullException>(() => new CapacityManager(azureClient, azureSubscriptionCatalog, azureSubscriptionCapacityProvider, controlPlaneInfo, null, capacitySettings));
            Assert.Throws<ArgumentNullException>(() => new CapacityManager(azureClient, azureSubscriptionCatalog, azureSubscriptionCapacityProvider, controlPlaneInfo, resourceNameBuilder, null));
        }

        [Fact]
        public async Task SelectAzureResourceLocation_Throws_NullArgumentException()
        {
            var capacityManager = CreateTestCapacityManager();
            var criteria = CoresCriteria();
            var logger = new Mock<IDiagnosticsLogger>().Object;

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                capacityManager.SelectAzureResourceLocation(null, AzureLocation.EastUs, logger));

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                capacityManager.SelectAzureResourceLocation(criteria, AzureLocation.EastUs, null));
        }

        [Fact]
        public async Task SelectAzureResourceLocation_Throws_LocationNotAvailableException()
        {
            var capacityManager = CreateTestCapacityManager();
            var criteria = CoresCriteria();
            var logger = new Mock<IDiagnosticsLogger>().Object;
 
            var badLocation = AzureLocation.EastUs2Euap;
            await Assert.ThrowsAsync<LocationNotAvailableException>(() =>
                capacityManager.SelectAzureResourceLocation(criteria, badLocation, logger));
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
            var capacityManager = CreateTestCapacityManager();
            var criteria = CoresCriteria();
            var logger = new Mock<IDiagnosticsLogger>().Object;

            var result = await capacityManager.SelectAzureResourceLocation(criteria, location, logger);
            var expectedSubscriptionNamePrefix = $"Mock-Subscription-{location}-0";
            Assert.StartsWith(expectedSubscriptionNamePrefix, result.Subscription.DisplayName);
            Assert.Equal(result.Location, location);
            Assert.True(result.Subscription.Enabled);
        }

        [Theory]
        [MemberData(nameof(GoodLocations))]
        public async Task SelectAzureResourceLocation_OutOfCapacity(AzureLocation location)
        {
            // All subscriptions, all regions report 99% used
            var capacityManager = CreateTestCapacityManager(0.99);
            var criteria = CoresCriteria();
            var logger = new Mock<IDiagnosticsLogger>().Object;

            await Assert.ThrowsAsync<CapacityNotAvailableException>(() => capacityManager.SelectAzureResourceLocation(criteria, location, logger));
        }

        [Theory]
        [MemberData(nameof(GoodLocations))]
        public async Task SelectAzureResourceLocation_OverCapacity_0(AzureLocation location)
        {
            // *-0 subscriptions report 100% full, *-1 subscriptions retport 80% full
            var capacityManager = CreateTestCapacityManager(0.8, fillZero: true);
            var criteria = CoresCriteria();
            var logger = new Mock<IDiagnosticsLogger>().Object;

            var result = await capacityManager.SelectAzureResourceLocation(criteria, location, logger);
            var expectedSubscriptionNamePrefix = $"Mock-Subscription-{location}-1";
            Assert.Equal(expectedSubscriptionNamePrefix, result.Subscription.DisplayName);
            Assert.Equal(result.Location, location);
            Assert.True(result.Subscription.Enabled);
        }


        private CapacityManager CreateTestCapacityManager(double percent = 0.10, bool fillZero = false)
        {
            var catalog = MockAzureSubscriptionCatalog;
            var capacityProvider = MockAzureSubscriptionCapacityProvider(percent, fillZero);
            var controlPlaneInfo = MockControlPlaneInfo();
            var capacitySettings = new CapacitySettings();
            var resourceNameBuilder = new ResourceNameBuilder(new DeveloperPersonalStampSettings(false));
            var azureClient = new Mock<IAzureClientFactory>().Object;
            var capacityManager = new CapacityManager(azureClient, catalog, capacityProvider, controlPlaneInfo, resourceNameBuilder, capacitySettings);
            return capacityManager;
        }

        private static readonly IAzureSubscriptionCatalog MockAzureSubscriptionCatalog = CreateMockAzureSubscriptionCatalog();

        private static IAzureSubscriptionCatalog CreateMockAzureSubscriptionCatalog()
        {
            var mockAzureSubscriptionCatalog = new Mock<IAzureSubscriptionCatalog>();
            mockAzureSubscriptionCatalog.Setup(it => it.AzureSubscriptions).Returns(
                new AzureSubscription[] 
                {
                    MockAzureSubscription(AzureLocation.EastUs, 0),
                    MockAzureSubscription(AzureLocation.SouthEastAsia, 0),
                    MockAzureSubscription(AzureLocation.WestEurope, 0),
                    MockAzureSubscription(AzureLocation.WestUs2, 0),
                    MockAzureSubscription(AzureLocation.EastUs, 1),
                    MockAzureSubscription(AzureLocation.SouthEastAsia, 1),
                    MockAzureSubscription(AzureLocation.WestEurope, 1),
                    MockAzureSubscription(AzureLocation.WestUs2, 1),
                    MockAzureSubscription(AzureLocation.EastUs, 2, false),
                    MockAzureSubscription(AzureLocation.SouthEastAsia, 2, false),
                    MockAzureSubscription(AzureLocation.WestEurope, 2, false),
                    MockAzureSubscription(AzureLocation.WestUs2, 2, false),
                });

            return mockAzureSubscriptionCatalog.Object;
        }

        private static AzureSubscription MockAzureSubscription(AzureLocation location, int instance = 0, bool enabled = true)
        {
            return new AzureSubscription(
                Guid.NewGuid().ToString(),
                $"Mock-Subscription-{location}-{instance}",
                new Mock<IServicePrincipal>().Object,
                enabled,
                new ReadOnlyCollection<AzureLocation>(
                    new AzureLocation[]
                    {
                        location
                    }),
                new ReadOnlyDictionary<string, int>(new Dictionary<string, int>
                {
                    { CoresQuota, 100 }
                }),
                new ReadOnlyDictionary<string, int>(new Dictionary<string, int>
                {
                    { "StorageAccounts", 100 }
                }),
                new ReadOnlyDictionary<string, int>(new Dictionary<string, int>
                {
                    { "VirtualNetworks", 100 }
                })
            );
        }


        private static IAzureSubscriptionCapacityProvider MockAzureSubscriptionCapacityProvider(double percent = 0.10, bool fillZero = false)
        {
            var mockAzureSubscriptionCapacityProvider = new Mock<IAzureSubscriptionCapacityProvider>();

            foreach (var subscription in MockAzureSubscriptionCatalog.AzureSubscriptions)
            {
                foreach (var location in subscription.Locations)
                {
                    var thisLimit = 100;
                    var thisCurrent = (int)(thisLimit * percent);

                    if (fillZero && subscription.DisplayName.EndsWith("-0"))
                    {
                        thisCurrent = thisLimit;
                    }
                    else if (subscription.DisplayName.EndsWith("-1"))
                    {
                        thisLimit /= 2;
                        thisCurrent /= 2;
                    }


                    mockAzureSubscriptionCapacityProvider
                        .Setup(obj =>
                            obj.LoadAzureResourceUsageAsync(
                                It.Is<IAzureSubscription>(sub => sub.SubscriptionId == subscription.SubscriptionId),
                                location,
                                ServiceType.Compute,
                                It.IsAny<IDiagnosticsLogger>()))
                        .ReturnsAsync(() =>
                            new AzureResourceUsage[]
                            {
                            new AzureResourceUsage(
                                subscription.SubscriptionId,
                                ServiceType.Compute,
                                location,
                                CoresQuota,
                                thisLimit,
                                thisCurrent),
                            });
                }
            }

            return mockAzureSubscriptionCapacityProvider.Object;
        }

        private const string CoresQuota = "cores";

        private static IControlPlaneInfo MockControlPlaneInfo()
        {
            var controlPlaneInfoMoq = new Mock<IControlPlaneInfo>();
            controlPlaneInfoMoq.Setup(obj => obj.Stamp.StampResourceGroupName).Returns("stamp-resource-group-000");
            return controlPlaneInfoMoq.Object;
        }

        private static IEnumerable<AzureResourceCriterion> CoresCriteria()
        {
            return new List<AzureResourceCriterion>
            {
                new AzureResourceCriterion
                {
                    ServiceType = ServiceType.Compute,
                    Quota = CoresQuota,
                    Required = 4
                }
            };
        }
    }
}
