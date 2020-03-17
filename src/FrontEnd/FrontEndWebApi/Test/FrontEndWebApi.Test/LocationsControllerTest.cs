using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Utility;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class LocationsControllerTest
    {
        private static readonly int[] DefaultAutoSuspendDelayMinutes = new int[] { 0, 5, 30, 120 };

        [Fact]
        public async Task GetLocationInfoAsync()
        {
            ICloudEnvironmentSku createMockSku(string name, ComputeOS os, decimal storageUnits, decimal computeUnits)
            {
                var currentImageInfoProvider = new Mock<ICurrentImageInfoProvider>();
                currentImageInfoProvider
                    .Setup(x => x.GetImageNameAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                    .Returns((ImageFamilyType familyType, string family, string defaultName, IDiagnosticsLogger logger) => Task.FromResult(defaultName));
                currentImageInfoProvider
                    .Setup(x => x.GetImageVersionAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                    .Returns((ImageFamilyType familyType, string family, string defaultVersion, IDiagnosticsLogger logger) => Task.FromResult(defaultVersion));

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
                        ImageFamilyType.VmAgent,
                        "agentImageFamily",
                        "agentImageName",
                        currentImageInfoProvider.Object),
                    new VmImageFamily(
                        MockControlPlaneInfo().Stamp,
                        "vmImageFamilyName",
                        VmImageKind.Canonical,
                        "vmImageName",
                        "vmImageVersion",
                        "vmImageSubscriptionId",
                        currentImageInfoProvider.Object),
                    "storageSkuName",
                    new BuildArtifactImageFamily(
                        ImageFamilyType.Storage,
                        "storageImageFamily",
                        "storageImageName",
                        currentImageInfoProvider.Object),
                    64,
                    storageUnits,
                    computeUnits,
                    5,
                    5,
                    new ReadOnlyCollection<string>(new string[0]),
                    new ReadOnlyCollection<string>(new string[0]),1);
            }

            var skuCatalog = new Mock<ISkuCatalog>();
            skuCatalog
                .Setup(obj => obj.CloudEnvironmentSkus)
                .Returns(() =>
                {
                    var skus = new[]
                    {
                        createMockSku("StandardLinux", ComputeOS.Linux, 100.0m, 100.0m),
                        createMockSku("premiumLinux", ComputeOS.Linux, 1.0m, 1.0m),
                        createMockSku("PremiuimWindows", ComputeOS.Windows, 1.0m, 1.0m),
                    };

                    var skuDict = skus.ToDictionary((s) => s.SkuName, (s) => s);
                    return new ReadOnlyDictionary<string, ICloudEnvironmentSku>(skuDict);
                });
            var logger = new Mock<IDiagnosticsLogger>().Object;

            var currentUserProvider = MockCurrentUserProvider(new Dictionary<string, object>
            {
                { ProfileExtensions.VisualStudioOnlineWindowsSkuPreviewUserProgram, true },
            });

            var skuUtils = MockUtil.MockSkuUtils(true);
            var planManager = MockUtil.MockPlanManager(() => MockUtil.GeneratePlan(location: AzureLocation.WestUs));

            var controller = CreateTestLocationsController(
                skuCatalog: skuCatalog.Object,
                currentUserProvider: currentUserProvider,
                skuUtils: skuUtils);

            var actionResult = await controller.GetAsync(AzureLocation.WestUs2.ToString(), logger);
            Assert.NotNull(actionResult);
            Assert.IsType<OkObjectResult>(actionResult);

            var okResult = (OkObjectResult)actionResult as OkObjectResult;
            var locationInfo = okResult.Value as LocationInfoResult;

            Assert.NotNull(locationInfo);
            Assert.Equal(3, locationInfo.Skus.Length);
            Assert.Equal("premiumLinux", locationInfo.Skus[1].Name);

            // Standard is always set as top priority when it comes to ordering.
            Assert.Equal("StandardLinux", locationInfo.Skus[0].Name); 
            Assert.Equal("PremiuimWindows", locationInfo.Skus[2].Name);
            Assert.Equal(DefaultAutoSuspendDelayMinutes, locationInfo.DefaultAutoSuspendDelayMinutes);
        }

        private LocationsController CreateTestLocationsController(ISkuCatalog skuCatalog, ISkuUtils skuUtils, ICurrentUserProvider currentUserProvider = null)
        {
            var controller = new LocationsController(
                MockCurrentLocationProvider(),
                MockControlPlaneInfo(),
                skuCatalog,
                currentUserProvider ?? MockCurrentUserProvider(),
                new PlanManagerSettings
                {
                    DefaultAutoSuspendDelayMinutesOptions = DefaultAutoSuspendDelayMinutes,
                },
                skuUtils);

            var httpContext = MockHttpContext.Create();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            httpContext.SetLogger(logger);
            httpContext.SetPlan("/subscriptions/8def34ce-053c-43ba-8501-37599fb7f010/resourceGroups/cloudEnvironments/providers/Microsoft.VSOnline/plans/samanoha-dev-stamp-plan");

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
                .Setup(obj => obj.GetCurrentUserIdSet())
                .Returns(new UserIdSet("mock-profile-id"));
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
