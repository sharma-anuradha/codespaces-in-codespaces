using System;
using System.Linq;
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
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class EnvironmentControllerTests
    {
        private readonly IPlanRepository accountRepository;
        private readonly PlanManager accountManager;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IDiagnosticsLogger logger;

        public EnvironmentControllerTests()
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
        public void EnvironmentController_Constructor()
        {
            var environmentController = CreateTestEnvironmentsController();
            Assert.NotNull(environmentController);
        }

        [Fact]
        public async Task EnvironmentController_CloudEnvironmentAsync()
        {
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var body = new CreateCloudEnvironmentBody
            {
                FriendlyName = "test-environment",
                PlanId = (await GeneratePlan()).Plan.ResourceId,
                AutoShutdownDelayMinutes = 5,
                Type = CloudEnvironmentType.CloudEnvironment.ToString(),
                SkuName = "testSkuName",
                Location = "WestUs2",
            };
            var environmentController = CreateTestEnvironmentsController();
            var actionResult = await environmentController.CreateAsync(body, logger);
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
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var skuCatalog = LoadSkuCatalog(environment);

            var currentUser = MockCurrentUserProvider(new Dictionary<string, object>
                                {
                                    { ProfileExtensions.VisualStudioOnlineWidowsSkuPreviewUserProgram, true },
                                },
                                "test@microsoft.com");

            var environmentController = CreateTestEnvironmentsController(skuCatalog: skuCatalog, currentUserProvider: currentUser);

            // Create each enabled sku in west us 2
            foreach (var item in skuCatalog.EnabledInternalHardware())
            {
                var skuName = item.Key;
                var sku = item.Value;
                var location = AzureLocation.WestUs2.ToString();
                var body = await CreateBodyAsync(skuName, location);
                var actionResult = await environmentController.CreateAsync(body, logger);
                Assert.IsType<CreatedResult>(actionResult);
            }
        }

        [Theory]
        [MemberData(nameof(Environments))]
        public async Task EnvironmentController_CloudEnvironmentAsync_SkuCatalog_BadRequest(string environment)
        {
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var skuCatalog = LoadSkuCatalog(environment);
            var environmentController = CreateTestEnvironmentsController(skuCatalog: skuCatalog);

            // Create each enabled sku in west us 2
            foreach (var item in skuCatalog.EnabledInternalHardware().Where((sku) => sku.Value.ComputeOS == ComputeOS.Windows))
            {
                var skuName = item.Key;
                var sku = item.Value;
                var location = AzureLocation.WestUs2.ToString();
                var body = await CreateBodyAsync(skuName, location);
                var actionResult = await environmentController.CreateAsync(body, logger);
                Assert.IsType<BadRequestObjectResult>(actionResult);
            }
        }

        [Fact]
        public async Task EnvironmentController_CloudEnvironmentAsync_BadResult()
        {
            var logger = new Mock<IDiagnosticsLogger>().Object;
            var skuCatalog = LoadSkuCatalog("prod-rel");

            var currentUser = MockCurrentUserProvider(new Dictionary<string, object>
            {
                { ProfileExtensions.VisualStudioOnlineWidowsSkuPreviewUserProgram, true },
            });

            var environmentController = CreateTestEnvironmentsController(skuCatalog: skuCatalog, currentUserProvider: currentUser);

            var skuName = skuCatalog.CloudEnvironmentSkus.Keys.FirstOrDefault();

            // Redirect location
            var body = await CreateBodyAsync(skuName, AzureLocation.EastUs.ToString());
            var actionResult = await environmentController.CreateAsync(body, logger);
            Assert.IsType<RedirectResult>(actionResult);

            // Not supported location
            body = await CreateBodyAsync(skuName, AzureLocation.AustraliaCentral.ToString());
            actionResult = await environmentController.CreateAsync(body, logger);
            Assert.IsType<BadRequestObjectResult>(actionResult);

            // Not supported SKU
            body = await CreateBodyAsync("bad-sku", AzureLocation.WestUs2.ToString());
            actionResult = await environmentController.CreateAsync(body, logger);
            Assert.IsType<BadRequestObjectResult>(actionResult);
        }

        public static TheoryData<CloudEnvironmentAvailableSettingsUpdates, CloudEnvironmentAvailableUpdatesResult> SettingsUpdates = 
            new TheoryData<CloudEnvironmentAvailableSettingsUpdates, CloudEnvironmentAvailableUpdatesResult>
            {
                { 
                    new CloudEnvironmentAvailableSettingsUpdates
                    {
                        AllowedAutoShutdownDelayMinutes = new int[0],
                        AllowedSkus = new ICloudEnvironmentSku[0],
                    },
                    new CloudEnvironmentAvailableUpdatesResult
                    {
                        AllowedAutoShutdownDelayMinutes = new int[0],
                        AllowedSkus = new SkuInfoResult[0],
                    }
                },
                {
                    new CloudEnvironmentAvailableSettingsUpdates
                    {
                        AllowedAutoShutdownDelayMinutes = new[] { 0, 5, 30, 120 },
                        AllowedSkus = new ICloudEnvironmentSku[0],
                    },
                    new CloudEnvironmentAvailableUpdatesResult
                    {
                        AllowedAutoShutdownDelayMinutes = new[] { 0, 5, 30, 120 },
                        AllowedSkus = new SkuInfoResult[0],
                    }
                },
                {
                    new CloudEnvironmentAvailableSettingsUpdates
                    {
                        AllowedAutoShutdownDelayMinutes = new int[0],
                        AllowedSkus = new ICloudEnvironmentSku[]
                        {
                            MockSku("MockSkuName", "MockSku", ComputeOS.Linux, 0, 0, new string[0]),
                        },
                    },
                    new CloudEnvironmentAvailableUpdatesResult
                    {
                        AllowedAutoShutdownDelayMinutes = new int[0],
                        AllowedSkus = new SkuInfoResult[]
                        {
                            new SkuInfoResult { Name = "MockSkuName" },
                        },
                    }
                },
            };

        [Theory]
        [MemberData(nameof(SettingsUpdates))]
        public async Task EnvironmentController_GetAvailableUpdatesAsync(
            CloudEnvironmentAvailableSettingsUpdates allowedUpdates,
            CloudEnvironmentAvailableUpdatesResult expectedResponse)
        {
            var logger = new Mock<IDiagnosticsLogger>().Object;

            var mockEnvironment = new CloudEnvironment
            {
                Id = Guid.NewGuid().ToString(),
                Location = AzureLocation.WestUs2,
            };

            var mockEnvironmentManager = new Mock<ICloudEnvironmentManager>();
            
            mockEnvironmentManager
                .Setup(x => x.GetEnvironmentAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockEnvironment))
                .Callback((string id, string user, IDiagnosticsLogger log) =>
                    Assert.Equal(mockEnvironment.Id, id));

            mockEnvironmentManager
                .Setup(x => x.GetEnvironmentAvailableSettingsUpdates(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<UserProfile.Profile>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(allowedUpdates);

            var environmentController = CreateTestEnvironmentsController(cloudEnvironmentManager: mockEnvironmentManager.Object);

            var actionResult = await environmentController.GetAvailableUpdatesAsync(mockEnvironment.Id, logger);
            Assert.IsType<OkObjectResult>(actionResult);

            var result = (actionResult as OkObjectResult).Value as CloudEnvironmentAvailableUpdatesResult;

            Assert.NotNull(result);
            Assert.Equal(expectedResponse.AllowedAutoShutdownDelayMinutes, result.AllowedAutoShutdownDelayMinutes);
            Assert.Equal(expectedResponse.AllowedSkus.Length, result.AllowedSkus.Length);

            for (var i = 0; i < expectedResponse.AllowedSkus.Length; ++i)
            {
                var expectedSku = expectedResponse.AllowedSkus[i];
                var actual = result.AllowedSkus[i];

                Assert.Equal(expectedSku.Name, actual.Name);
            }
        }

        [Fact]
        public async Task EnvironmentController_UpdateSettingsAsync()
        {
            var logger = new Mock<IDiagnosticsLogger>().Object;

            var mockEnvironment = new CloudEnvironment
            {
                Id = Guid.NewGuid().ToString(),
                Location = AzureLocation.WestUs2,
            };

            var updateRequest = new UpdateCloudEnvironmentBody
            {
                SkuName = "MockSku",
                AutoShutdownDelayMinutes = 123,
            };

            var updateSettingsReponse = CloudEnvironmentSettingsUpdateResult.Success(mockEnvironment);

            var mockEnvironmentManager = new Mock<ICloudEnvironmentManager>();

            mockEnvironmentManager
                .Setup(x => x.GetEnvironmentAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockEnvironment))
                .Callback((string id, string user, IDiagnosticsLogger log) =>
                    Assert.Equal(mockEnvironment.Id, id));

            mockEnvironmentManager
                .Setup(x => x.UpdateEnvironmentSettingsAsync(
                    It.IsAny<string>(),
                    It.IsAny<CloudEnvironmentUpdate>(),
                    It.IsAny<ICurrentUserProvider>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(updateSettingsReponse))
                .Callback((string id, CloudEnvironmentUpdate update, ICurrentUserProvider prov, IDiagnosticsLogger log) =>
                {
                    Assert.Equal(updateRequest.SkuName, update.SkuName);
                    Assert.Equal(updateRequest.AutoShutdownDelayMinutes, update.AutoShutdownDelayMinutes);
                });

            var environmentController = CreateTestEnvironmentsController(cloudEnvironmentManager: mockEnvironmentManager.Object);

            var actionResult = await environmentController.UpdateSettingsAsync(mockEnvironment.Id, updateRequest, logger);
            Assert.IsType<OkObjectResult>(actionResult);

            var result = (actionResult as OkObjectResult).Value as CloudEnvironmentResult;

            Assert.NotNull(result);
            Assert.Equal(mockEnvironment.Id, result.Id);
        }

        [Fact]
        public async Task EnvironmentController_UpdateSettingsAsync_BadRequest()
        {
            var logger = new Mock<IDiagnosticsLogger>().Object;

            var mockEnvironment = new CloudEnvironment
            {
                Id = Guid.NewGuid().ToString(),
                Location = AzureLocation.WestUs2,
            };

            var updateRequest = new UpdateCloudEnvironmentBody
            {
                SkuName = "MockSku",
                AutoShutdownDelayMinutes = 123,
            };

            var errorCodes = new List<EnvironmentManager.Contracts.MessageCodes> { EnvironmentManager.Contracts.MessageCodes.EnvironmentNotShutdown, EnvironmentManager.Contracts.MessageCodes.RequestedSkuIsInvalid, };
            var updateSettingsReponse = CloudEnvironmentSettingsUpdateResult.Error(errorCodes);

            var mockEnvironmentManager = new Mock<ICloudEnvironmentManager>();

            mockEnvironmentManager
                .Setup(x => x.GetEnvironmentAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(mockEnvironment))
                .Callback((string id, string user, IDiagnosticsLogger log) =>
                    Assert.Equal(mockEnvironment.Id, id));

            mockEnvironmentManager
                .Setup(x => x.UpdateEnvironmentSettingsAsync(
                    It.IsAny<string>(),
                    It.IsAny<CloudEnvironmentUpdate>(),
                    It.IsAny<ICurrentUserProvider>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(updateSettingsReponse))
                .Callback((string id, CloudEnvironmentUpdate update, ICurrentUserProvider prov, IDiagnosticsLogger log) =>
                {
                    Assert.Equal(updateRequest.SkuName, update.SkuName);
                    Assert.Equal(updateRequest.AutoShutdownDelayMinutes, update.AutoShutdownDelayMinutes);
                });

            var environmentController = CreateTestEnvironmentsController(cloudEnvironmentManager: mockEnvironmentManager.Object);

            var actionResult = await environmentController.UpdateSettingsAsync(mockEnvironment.Id, updateRequest, logger);
            Assert.IsType<BadRequestObjectResult>(actionResult);

            var result = (actionResult as BadRequestObjectResult).Value as List<EnvironmentManager.Contracts.MessageCodes>;

            Assert.NotNull(result);
            Assert.Equal(errorCodes.Count, result.Count);

            for (var i = 0; i < errorCodes.Count; ++i)
            {
                var expectedCode = errorCodes[i];
                var actualCode = result[i];

                Assert.Equal(expectedCode, actualCode);
            }
        }

        private async Task<CreateCloudEnvironmentBody> CreateBodyAsync(string skuName, string location)
        {
            return new CreateCloudEnvironmentBody
            {
                FriendlyName = "test-environment",
                PlanId = (await GeneratePlan()).Plan.ResourceId,
                AutoShutdownDelayMinutes = 5,
                Type = CloudEnvironmentType.CloudEnvironment.ToString(),
                SkuName = skuName,
                Location = location,
            };
        }

        private EnvironmentsController CreateTestEnvironmentsController(
            ISkuCatalog skuCatalog = null, 
            ICurrentUserProvider currentUserProvider = null,
            ICloudEnvironmentManager cloudEnvironmentManager = null)
        {
            cloudEnvironmentManager = cloudEnvironmentManager ?? MockCloudEnvironmentManager();
            currentUserProvider = currentUserProvider ?? MockCurrentUserProvider();
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

            var httpContext = MockHttpContext.Create();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            httpContext.SetLogger(logger);

            environmentController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
            };

            return environmentController;
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
                cfg.CreateMap<EnvironmentRegistrationCallbackPayloadBody, EnvironmentRegistrationCallbackPayloadOptions>();
            });

            var mapper = new Mapper(config);

            return mapper;
        }

        private ISkuCatalog LoadSkuCatalog(string environmentName)
        {
            var appSettings = LoadAppSettings(environmentName);
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

        private static ICloudEnvironmentSku MockSku(
            string skuName, 
            string displayName, 
            ComputeOS computeOs,
            decimal storageUnits, 
            decimal computeUnits, 
            IEnumerable<string> skuTransitions)
        {
            return new CloudEnvironmentSku(
                skuName,
                SkuTier.Standard,
                displayName,
                true,
                new[] { AzureLocation.WestUs2 },
                "computeSkuFamily",
                "computeSkuName",
                "computeSkuSize",
                4,
                computeOs,
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
                new ReadOnlyCollection<string>(skuTransitions.ToList()));
        }

        private ISkuCatalog MockSkuCatalog()
        {
            return MockSkuCatalog(new CloudEnvironmentSku(
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
                2.0m,
                125.0m,
                5,
                5,
                new ReadOnlyCollection<string>(new string[0])));
        }

        private ISkuCatalog MockSkuCatalog(params ICloudEnvironmentSku[] skus)
        {
            var skuDict = new ReadOnlyDictionary<string, ICloudEnvironmentSku>(skus.ToDictionary((s) => s.SkuName));

            var moq = new Mock<ISkuCatalog>();
            moq
                .Setup(obj => obj.CloudEnvironmentSkus)
                .Returns(skuDict);

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

        private static IControlPlaneInfo MockControlPlaneInfo()
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

        private ICurrentUserProvider MockCurrentUserProvider(Dictionary<string, object> programs = null, string email = default )
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
                        Programs = programs,
                        Email = email
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
                        MessageCode = 0,
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

        private async Task<VsoPlan> GeneratePlan(string planName = "Test")
        {
            var model = new VsoPlan
            {
                Plan = new VsoPlanInfo
                {
                    Name = planName,
                    ResourceGroup = "myRG",
                    Subscription = Guid.NewGuid().ToString(),
                    Location = AzureLocation.WestUs2
                },
                UserId = "TestUser",
            };

            var serviceResult = await accountManager.CreateAsync(model, logger);
            Assert.Equal(Plans.Contracts.ErrorCodes.Unknown, serviceResult.ErrorCode);
            Assert.NotNull(serviceResult.VsoPlan);

            return serviceResult.VsoPlan;
        }
    }
}
