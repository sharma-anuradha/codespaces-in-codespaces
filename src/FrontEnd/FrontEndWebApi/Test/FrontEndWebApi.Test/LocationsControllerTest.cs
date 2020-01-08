using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;
using System.Linq;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public partial class LocationsControllerTest
    {
        private readonly IPlanRepository accountRepository;
        private readonly PlanManager accountManager;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IDiagnosticsLogger logger;

        public LocationsControllerTest()
        {
            loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();

            var planSettings = new PlanManagerSettings() { DefaultMaxPlansPerSubscription = 20 };

            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<int>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), planSettings.DefaultMaxPlansPerSubscription))
                .Returns(Task.FromResult(planSettings.DefaultMaxPlansPerSubscription));

            planSettings.Init(mockSystemConfiguration.Object);

            accountRepository = new MockPlanRepository();
            accountManager = new PlanManager(accountRepository, planSettings);
        }

        [Fact]
        public void GetLocationInfo()
        {
            ICloudEnvironmentSku createMockSku(string name, ComputeOS os, decimal storageUnits, decimal computeUnits)
            {
                return new CloudEnvironmentSku(                
                    name,
                    SkuTier.Standard,
                    "Test SKU Name",
                    true,
                    new[] { AzureLocation.WestUs2 },
                    "computeSkuFamily",
                    "computeSkuName",
                    "computeSkuSize",
                    4,
                    os,
                    new BuildArtifactImageFamily(
                        "agentImageFamily",
                        "agentImageName"),
                    new VmImageFamily(
                        MockControlPlaneInfo().Stamp,
                        "vmImageFamilyName",
                        VmImageKind.Canonical,
                        "vmImageName",
                        "vmImageVersion",
                        "vmImageSubscriptionId"),
                    "storageSkuName",
                    new BuildArtifactImageFamily(
                        "storageImageFamily",
                        "storageImageName"),
                    64,
                    storageUnits,
                    computeUnits,
                    5,
                    5,
                    new ReadOnlyCollection<string>(new string[0]));
            }

            var skuCatalog = new Mock<ISkuCatalog>();
            skuCatalog
                .Setup(obj => obj.CloudEnvironmentSkus)
                .Returns(() =>
                {
                    var skus = new[]
                    {
                        createMockSku("premiumLinux", ComputeOS.Linux, 100.0m, 100.0m),
                        createMockSku("windows", ComputeOS.Windows, 1.0m, 1.0m),
                        createMockSku("cheapLinux", ComputeOS.Linux, 1.0m, 1.0m),
                    };

                    var skuDict = skus.ToDictionary((s) => s.SkuName, (s) => s);
                    return new ReadOnlyDictionary<string, ICloudEnvironmentSku>(skuDict);
                });
            var logger = new Mock<IDiagnosticsLogger>().Object;

            var currentUserProvider = MockCurrentUserProvider(new Dictionary<string, object>
            {
                { ProfileExtensions.VisualStudioOnlineWidowsSkuPreviewUserProgram, true },
            });

            var controller = CreateTestLocationsController(
                skuCatalog: skuCatalog.Object, 
                currentUserProvider: currentUserProvider);

            var result = controller.Get(AzureLocation.WestUs2.ToString(), logger);
            Assert.NotNull(result as OkObjectResult);

            var locationInfo = (result as OkObjectResult).Value as LocationInfoResult;
            Assert.NotNull(locationInfo);

            Assert.Equal(3, locationInfo.Skus.Length);

            Assert.Equal("cheapLinux", locationInfo.Skus[0].Name);
            Assert.Equal("premiumLinux", locationInfo.Skus[1].Name);
            Assert.Equal("windows", locationInfo.Skus[2].Name);
        }

        private LocationsController CreateTestLocationsController(ISkuCatalog skuCatalog, ICurrentUserProvider currentUserProvider = null)
        {
            var controller = new LocationsController(
                MockCurrentLocationProvider(),
                MockControlPlaneInfo(),
                skuCatalog,
                currentUserProvider ?? MockCurrentUserProvider());

            var httpContext = MockHttpContext.Create();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            httpContext.SetLogger(logger);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
            };

            return controller;
        }

        private ICurrentLocationProvider MockCurrentLocationProvider()
        {
            var moq = new Mock<ICurrentLocationProvider>();
            moq
                .Setup(obj => obj.CurrentLocation)
                .Returns(AzureLocation.WestUs2);

            return moq.Object;
        }

        private IControlPlaneInfo MockControlPlaneInfo()
        {
            var moq = new Mock<IControlPlaneInfo>();
            var moq2 = new Mock<IControlPlaneStampInfo>();
            moq
                .Setup(obj => obj.GetOwningControlPlaneStamp(It.IsAny<AzureLocation>()))
                .Returns((AzureLocation location) =>
                {
                    if (location == AzureLocation.WestUs2 || location == AzureLocation.EastUs)
                    {
                        moq2
                            .Setup(obj2 => obj2.Location)
                            .Returns(location);
                        return moq2.Object;
                    }

                    throw new NotSupportedException();
                });
            moq
                .Setup(obj => obj.Stamp)
                .Returns(moq2.Object);

            return moq.Object;

        }

        private ICurrentUserProvider MockCurrentUserProvider(Dictionary<string, object> programs = null)
        {
            var moq = new Mock<ICurrentUserProvider>();
            moq
                .Setup(obj => obj.GetProfileId())
                .Returns("mock-profile-id");
            moq
                .Setup(obj => obj.GetBearerToken())
                .Returns("mock-bearer-token");
            moq
                .Setup(obj => obj.GetProfile())
                .Returns(() =>
                {
                    return new UserProfile.Profile
                    {
                        ProviderId = "mock-provider-id",
                        Programs = programs
                    };
                });

            return moq.Object;
        }
    }
}
