using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class EnvironmentControllerTests
    {
        [Fact]
        public void EnvironmentController_Constructor()
        {
            var environmentController = CreateTestEnvironmentsController();
            Assert.NotNull(environmentController);
        }

        [Fact]
        public async Task EnvironmentController_CloudEnvironmentAsync()
        {
            var body = new CreateCloudEnvironmentBody 
            {
                FriendlyName = "test-environment",
                AccountId = "fake-account-id",
                AutoShutdownDelayMinutes = 5,
                Type = CloudEnvironmentType.CloudEnvironment.ToString(),
            };
            var environmentController = CreateTestEnvironmentsController();
            var actionResult = await environmentController.CreateCloudEnvironmentAsync(body);
            Assert.IsType<CreatedResult>(actionResult);
        }

        [Fact]
        public async Task EnvironmentController_CloudEnvironmentAsync_LegacySkus()
        {
            var skuCatalog = LoadSkuCatalog("prod-rel");
            var environmentController = CreateTestEnvironmentsController(skuCatalog);

            var body = CreateBody("smallLinuxPreview", null);
            var actionResult = await environmentController.CreateCloudEnvironmentAsync(body);
            Assert.IsType<CreatedResult>(actionResult);

            body = CreateBody("smallWindowsPreview", null);
            actionResult = await environmentController.CreateCloudEnvironmentAsync(body);
            Assert.IsType<CreatedResult>(actionResult);
        }

        public static TheoryData<string> Environments = new TheoryData<string>
        {
            "dev-ci",
            "ppe-rel",
            "prod-rel",
        };

        [Theory]
        [MemberData(nameof(Environments))]
        public async Task EnvironmentController_CloudEnvironmentAsync_SkuCatalog(string environment)
        {
            var skuCatalog = LoadSkuCatalog(environment);
            var environmentController = CreateTestEnvironmentsController(skuCatalog);

            // Create each enabled sku in west us 2
            foreach (var item in skuCatalog.CloudEnvironmentSkus)
            {
                var skuName = item.Key;
                var sku = item.Value;
                var location = AzureLocation.WestUs2.ToString();
                if (sku.Enabled)
                {
                    var body = CreateBody(skuName, location);
                    var actionResult = await environmentController.CreateCloudEnvironmentAsync(body);
                    Assert.IsType<CreatedResult>(actionResult);
                }
            }
        }

        [Fact]
        public async Task EnvironmentController_CloudEnvironmentAsync_BadResult()
        {
            var skuCatalog = LoadSkuCatalog("prod-rel");
            var environmentController = CreateTestEnvironmentsController(skuCatalog);

            // Redirect location
            var body = CreateBody(null, AzureLocation.EastUs.ToString());
            var actionResult = await environmentController.CreateCloudEnvironmentAsync(body);
            Assert.IsType<RedirectResult>(actionResult);

            // Not supported location
            body = CreateBody(null, AzureLocation.AustraliaCentral.ToString());
            actionResult = await environmentController.CreateCloudEnvironmentAsync(body);
            Assert.IsType<BadRequestObjectResult>(actionResult);

            // Not supported SKU
            body = CreateBody("bad-sku", AzureLocation.WestUs2.ToString());
            actionResult = await environmentController.CreateCloudEnvironmentAsync(body);
            Assert.IsType<BadRequestObjectResult>(actionResult);
        }

        private static CreateCloudEnvironmentBody CreateBody(string skuName, string location)
        {
            return new CreateCloudEnvironmentBody
            {
                FriendlyName = "test-environment",
                AccountId = "fake-account-id",
                AutoShutdownDelayMinutes = 5,
                Type = CloudEnvironmentType.CloudEnvironment.ToString(),
                SkuName = skuName,
                Location = location,
            };
        }

        private EnvironmentsController CreateTestEnvironmentsController(ISkuCatalog skuCatalog = null)
        {
            var cloudEnvironmentManager = MockCloudEnvironmentManager();
            var currentUserProvider = MockCurrentUserProvider();
            var controlPlaneInfo = MockControlPlaneInfo();
            var currentLocationProvider = MockCurrentLocationProvider();
            skuCatalog = skuCatalog ?? MockSkuCatalog();
            var mapper = MockMapper();
            var serviceUriBuilder = MockServiceUriBuilder();

            var environmentController = new EnvironmentsController(
                cloudEnvironmentManager,
                currentUserProvider,
                controlPlaneInfo,
                currentLocationProvider,
                skuCatalog,
                mapper,
                serviceUriBuilder);

            var httpContext = new MockHttpContext();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            httpContext.SetLogger(logger);

            environmentController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
            };

            return environmentController;
        }

        private class MockHttpContext : DefaultHttpContext
        {
            public MockHttpContext()
            {
                Request.Method = "GET";
                Request.Scheme = "https";
                Request.Host = new HostString("testhost");
                Request.PathBase = new PathString(string.Empty);
                Request.Path = new PathString("/test/path");
                Request.QueryString = new QueryString(string.Empty);
            }
        }

        private IServiceUriBuilder MockServiceUriBuilder()
        {
            var moq = new Mock<IServiceUriBuilder>();
            moq
                .Setup(obj => obj.GetServiceUri(It.IsAny<string>(), It.IsAny<IControlPlaneStampInfo>()))
                .Returns(new Uri("https://testhost/test-service-uri/"));
            moq
                .Setup(obj => obj.GetCallbackUriFormat(It.IsAny<string>(), It.IsAny<IControlPlaneStampInfo>()))
                .Returns(new Uri("https://testhost/test-callback-uri/"));

            return moq.Object;
        }

        private IMapper MockMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CloudEnvironment, CloudEnvironmentResult>();
                cfg.CreateMap<CreateCloudEnvironmentBody, CloudEnvironment>();
                cfg.CreateMap<SeedInfoBody, SeedInfo>();
                cfg.CreateMap<GitConfigOptionsBody, GitConfigOptions>();
                cfg.CreateMap<EnvironmentRegistrationCallbackBody, EnvironmentRegistrationCallbackOptions>();
                cfg.CreateMap<EnvironmetnRegistrationCallbackPayloadBody, EnvironmentRegistrationCallbackPayloadOptions>();
            });

            var mapper = new Mapper(config);

            return mapper;
        }

        private ISkuCatalog LoadSkuCatalog(string environmentName)
        {
            var appSettings = LoadAppSettings(environmentName);

            // Hack for Windows SKUs. They are temporary disabled in PPE and Prod.
            // But the unit tests will act as if they are enabled for all environments.
            appSettings.SkuCatalogSettings.CloudEnvironmentSkuSettings["standardWindows"].Enabled = true;
            appSettings.SkuCatalogSettings.CloudEnvironmentSkuSettings["premiumWindows"].Enabled = true;

            var controlPlaneInfo = new Mock<IControlPlaneInfo>();
            controlPlaneInfo
                .Setup(obj => obj.EnvironmentResourceGroupName)
                .Returns("test-environment-rg");
            controlPlaneInfo
                .Setup(obj => obj.Stamp.DataPlaneLocations)
                .Returns(new List<AzureLocation> 
                {
                    AzureLocation.WestUs2,
                });

            var subscriptionId = Guid.NewGuid().ToString();
            var controlPlaneAzureResourceAccessor = new Mock<IControlPlaneAzureResourceAccessor>();
            controlPlaneAzureResourceAccessor
                .Setup(obj => obj.GetCurrentSubscriptionIdAsync())
                .Returns(Task.FromResult(subscriptionId));

            var skuCatalog = new SkuCatalog(
                appSettings.SkuCatalogSettings,
                controlPlaneInfo.Object,
                controlPlaneAzureResourceAccessor.Object);

            return skuCatalog;
        }

        private ISkuCatalog MockSkuCatalog()
        {
            var moq = new Mock<ISkuCatalog>();
            moq
                .Setup(obj => obj.CloudEnvironmentSkus)
                .Returns(() =>
                {
                    var sku = new CloudEnvironmentSku(
                        "testSkuName",
                        SkuTier.Standard,
                        "Test SKU Name",
                        true,
                        new[] { AzureLocation.WestUs2 },
                        "computeSkuFamily",
                        "computeSkuName",
                        "computeSkuSize",
                        4,
                        ComputeOS.Linux,
                        new BuildArtifactImageFamily(
                            "agentImageFamily",
                            "agentImageName"),
                        new VmImageFamily(
                            "vmImageFamilyName",
                            VmImageKind.Canonical,
                            "vmImageName",
                            "vmImageVersion",
                            "vmImageSubscriptionId",
                            "vmImageResourceGroup"),
                        "storageSkuName",
                        new BuildArtifactImageFamily(
                            "storageImageFamily",
                            "storageImageName"),
                        64,
                        2.0m,
                        125.0m,
                        5,
                        5);

                    var skus = new Dictionary<string, ICloudEnvironmentSku>();
                    skus.Add(sku.SkuName, sku);
                    return new ReadOnlyDictionary<string, ICloudEnvironmentSku>(skus);
                });

            return moq.Object;
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
            moq
                .Setup(obj => obj.GetOwningControlPlaneStamp(It.IsAny<AzureLocation>()))
                .Returns((AzureLocation location) => 
                {
                    if (location == AzureLocation.WestUs2 || location == AzureLocation.EastUs)
                    {
                        var moq2 = new Mock<IControlPlaneStampInfo>();
                        moq2
                            .Setup(obj2 => obj2.Location)
                            .Returns(location);
                        return moq2.Object;
                    }

                    throw new NotSupportedException();
                });

            return moq.Object;

        }

        private ICurrentUserProvider MockCurrentUserProvider()
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
                    };
                });

            return moq.Object;
        }

        private ICloudEnvironmentManager MockCloudEnvironmentManager()
        {
            var moq = new Mock<ICloudEnvironmentManager>();

            moq
                .Setup(obj => obj.CreateEnvironmentAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<CloudEnvironmentOptions>(),
                    It.IsAny<Uri>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync((
                    CloudEnvironment env,
                    CloudEnvironmentOptions options,
                    Uri uri,
                    string s1,
                    string s2,
                    string s3,
                    string s4,
                    IDiagnosticsLogger logger) =>
                {
                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = env,
                        ErrorCode = 0,
                        HttpStatusCode = StatusCodes.Status200OK,
                    };
                });

            return moq.Object;
        }

        public static AppSettingsBase LoadAppSettings(string environmentName, string overrideName = null)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.images.json", optional: false)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: false);

            if (!string.IsNullOrEmpty(overrideName))
            {
                builder.AddJsonFile($"appsettings.{overrideName}.json", optional: false);
            }

            var configuration = builder.Build();
            var appSettingsConfiguration = configuration.GetSection("AppSettings");
            var appSettings = appSettingsConfiguration.Get<AppSettingsBase>();
            return appSettings;
        }
    }
}
