using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;
using Profile = Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.Profile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public static class MockUtil
    {
        public static readonly string MockEnvironmentId = Guid.NewGuid().ToString();
        public const string MockServiceUri = "https://testhost/test-service-uri/";
        private const string MockCallbackUriFormat = "https://testhost/test-callback-uri/";
        private const string MockUserProviderId = "mock-provider-id";

        public static CloudEnvironment MockCloudEnvironment(
            string ownerId = null,
            string planId = null)
        {
            return new CloudEnvironment
            {
                Id = MockEnvironmentId,
                Location = AzureLocation.WestUs2,
                OwnerId = ownerId ?? MockCurrentUserProvider().CurrentUserIdSet?.PreferredUserId,
                PlanId = planId,
                Connection = new EnvironmentManager.ConnectionInfo(),
            };
        }

        public static IServiceUriBuilder MockServiceUriBuilder(string expectedRequestUri = null)
        {
            var moq = new Mock<IServiceUriBuilder>();
            moq
                .Setup(obj => obj.GetServiceUri(It.IsAny<string>(), It.IsAny<IControlPlaneStampInfo>()))
                .Returns(new Uri(MockServiceUri))
                .Callback((string requestUri, IControlPlaneStampInfo ctrlPlane) =>
                {
                    if (expectedRequestUri != null)
                    {
                        Assert.Equal(expectedRequestUri, requestUri);
                    }
                });
            moq
                .Setup(obj => obj.GetCallbackUriFormat(It.IsAny<string>(), It.IsAny<IControlPlaneStampInfo>()))
                .Returns(new Uri(MockCallbackUriFormat))
                .Callback((string requestUri, IControlPlaneStampInfo ctrlPlane) =>
                {
                    if (expectedRequestUri != null)
                    {
                        Assert.Equal(expectedRequestUri, requestUri);
                    }
                });

            return moq.Object;
        }

        public static ISystemConfiguration MockSystemConfiguration()
        {
            var moq = new Mock<ISystemConfiguration>();
            moq.SetReturnsDefault(Task.FromResult(true));
            return moq.Object;
        }

        public static IDiagnosticsLogger MockLogger()
        {
            var moq = new Mock<IDiagnosticsLogger>();
            return moq.Object;
        }

        public static IMetricsManager MockMetricsManager()
        {
            var moq = new Mock<IMetricsManager>();

            moq.Setup(x => x.GetMetricsInfoForRequestAsync(It.IsAny<IHeaderDictionary>(), It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync((IHeaderDictionary hd, IDiagnosticsLogger logger) => new MetricsClientInfo(null, null));

            return moq.Object;
        }

        public static ISubscriptionManager MockSubscriptionManager()
        {
            var moq = new Mock<ISubscriptionManager>();
            moq.Setup(t => t.CanSubscriptionCreatePlansAndEnvironmentsAsync(It.IsAny<Subscription>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(true));
            return moq.Object;
        }

        public static IEnvironmentAccessManager MockEnvironmentAccessManager()
        {
            var moq = new Mock<IEnvironmentAccessManager>();
            moq.Setup(t => t.AuthorizeEnvironmentAccess(It.IsAny<CloudEnvironment>(), It.IsAny<string[]>(), It.IsAny<IDiagnosticsLogger>()));
            moq.Setup(t => t.AuthorizePlanAccess(It.IsAny<VsoPlan>(), It.IsAny<string[]>(), It.IsAny<VsoClaimsIdentity>(), It.IsAny<IDiagnosticsLogger>()));
            return moq.Object;
        }

        public static ISkuUtils MockSkuUtils(bool value)
        {
            var moq = new Mock<ISkuUtils>();
            moq.Setup(x => x.IsVisible(It.IsAny<CloudEnvironmentSku>(), It.IsAny<VsoPlanInfo>(), It.IsAny<Profile>()))
               .Returns((CloudEnvironmentSku sku, VsoPlanInfo planInfo, Profile profile) => Task.FromResult(value));
            return moq.Object;
        }

        public static IMapper MockMapper()
        {
            // Use a temporary service provider to construct and get the FrontEnd model mapper.
            var factory = new DefaultServiceProviderFactory();
            var serviceCollection = factory.CreateBuilder(new ServiceCollection());

            serviceCollection.AddSingleton(new FrontEndAppSettings
            {
                VSLiveShareApiEndpoint = MockServiceUri,
            });
            serviceCollection.AddSingleton(MockSkuCatalog());
            serviceCollection.AddModelMapper();

            var serviceProvider = factory.CreateServiceProvider(serviceCollection);
            return serviceProvider.GetService<IMapper>();
        }

        public static ICloudEnvironmentSku MockSku(
            string skuName,
            SkuTier skuTier,
            string displayName,
            ComputeOS computeOs,
            decimal storageUnits,
            decimal computeUnits,
            IEnumerable<string> skuTransitions,
            IEnumerable<string> supportedFeatures,
            int priority)
        {
            var currentImageInfoProvider = new Mock<ICurrentImageInfoProvider>();
            currentImageInfoProvider
                .Setup(x => x.GetImageNameAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultName, IDiagnosticsLogger logger) => Task.FromResult(defaultName));
            currentImageInfoProvider
                .Setup(x => x.GetImageVersionAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultVersion, IDiagnosticsLogger logger) => Task.FromResult(defaultVersion));

            return new CloudEnvironmentSku(
                skuName,
                skuTier,
                displayName,
                true,
                new[] { AzureLocation.WestUs2 },
                "computeSkuFamily",
                "computeSkuName",
                "computeSkuSize",
                4,
                computeOs,
                new BuildArtifactImageFamily(
                    ImageFamilyType.VmAgent,
                    "agentImageFamily",
                    "agentImageName",
                    "computeImageVersion",
                    currentImageInfoProvider.Object),
                new VmImageFamily(
                    MockControlPlaneInfo().Stamp,
                    "vmImageFamilyName",
                    ImageKind.Canonical,
                    "vmImageName",
                    "vmImageVersion",
                    "vmImageSubscriptionId",
                    currentImageInfoProvider.Object),
                "storageSkuName",
                new BuildArtifactImageFamily(
                    ImageFamilyType.Storage,
                    "storageImageFamily",
                    "storageImageName",
                    null,
                    currentImageInfoProvider.Object),
                64,
                storageUnits,
                computeUnits,
                5,
                5,
                new ReadOnlyCollection<string>(skuTransitions.ToList()),
                new ReadOnlyCollection<string>(supportedFeatures.ToList()),
                priority);
        }

        public static ISkuCatalog MockSkuCatalog()
        {
            var currentImageInfoProvider = new Mock<ICurrentImageInfoProvider>();
            currentImageInfoProvider
                .Setup(x => x.GetImageNameAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultName, IDiagnosticsLogger logger) => Task.FromResult(defaultName));
            currentImageInfoProvider
                .Setup(x => x.GetImageVersionAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultVersion, IDiagnosticsLogger logger) => Task.FromResult(defaultVersion));

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
                    ImageFamilyType.VmAgent,
                    "agentImageFamily",
                    "agentImageName",
                    "computeImageVersion",
                    currentImageInfoProvider.Object),
                new VmImageFamily(
                    MockControlPlaneInfo().Stamp,
                    "vmImageFamilyName",
                    ImageKind.Canonical,
                    "vmImageName",
                    "vmImageVersion",
                    "vmImageSubscriptionId",
                    currentImageInfoProvider.Object),
                "storageSkuName",
                new BuildArtifactImageFamily(
                    ImageFamilyType.Storage,
                    "storageImageFamily",
                    "storageImageName",
                    null,
                    currentImageInfoProvider.Object),
                64,
                2.0m,
                125.0m,
                5,
                5,
                new ReadOnlyCollection<string>(new string[0]),
                new ReadOnlyCollection<string>(new string[0]),
                1));
        }

        public static ISkuCatalog MockSkuCatalog(params ICloudEnvironmentSku[] skus)
        {
            var skuDict = new ReadOnlyDictionary<string, ICloudEnvironmentSku>(skus.ToDictionary((s) => s.SkuName));

            var moq = new Mock<ISkuCatalog>();
            moq
                .Setup(obj => obj.CloudEnvironmentSkus)
                .Returns(skuDict);

            return moq.Object;
        }

        public static ICurrentLocationProvider MockCurrentLocationProvider()
        {
            var moq = new Mock<ICurrentLocationProvider>();
            moq
                .Setup(obj => obj.CurrentLocation)
                .Returns(AzureLocation.WestUs2);

            return moq.Object;
        }

        public static IControlPlaneInfo MockControlPlaneInfo()
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

        public static ICurrentUserProvider MockCurrentUserProvider(
            Dictionary<string, object> programs = null,
            string email = default,
            VsoClaimsIdentity identity = null)
        {
            var moq = new Mock<ICurrentUserProvider>();
            moq.Setup(obj => obj.CurrentUserIdSet)
                .Returns(new UserIdSet("mock-profile-id"));
            moq.Setup(obj => obj.BearerToken)
                .Returns("mock-bearer-token");
            moq.Setup(obj => obj.GetProfileAsync())
                .Returns(() =>
                {
                    return Task.FromResult(new Profile
                    {
                        ProviderId = MockUserProviderId,
                        Programs = programs,
                        Email = email
                    });
                });
            moq.Setup(obj => obj.Identity)
                .Returns(identity ?? new VsoClaimsIdentity(null, null, null, new ClaimsIdentity()));

            return moq.Object;
        }

        public static IEnvironmentManager MockEnvironmentManager(CloudEnvironment environment = null)
        {
            var moq = new Mock<IEnvironmentManager>();

            moq
                .Setup(obj => obj.ListAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<UserIdSet>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync((
                    string planId,
                    string name,
                    UserIdSet userIdSet,
                    IDiagnosticsLogger logger) =>
                {
                    var mockEnv = MockCloudEnvironment();
                    return new[] { mockEnv };
                });

            moq
                .Setup(obj => obj.CreateAsync(
                    It.IsAny<EnvironmentCreateDetails>(),
                    It.IsAny<StartCloudEnvironmentParameters>(),
                    It.IsAny<MetricsInfo>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync((
                    EnvironmentCreateDetails envCreateDetails,
                    StartCloudEnvironmentParameters startParams,
                    MetricsInfo metricsInfo,
                    IDiagnosticsLogger logger) =>
                {
                    Assert.Equal(MockServiceUri, startParams.FrontEndServiceUri.ToString());
                    Assert.Equal(MockCallbackUriFormat, startParams.CallbackUriFormat);

                    envCreateDetails.Connection = new ConnectionInfoBody
                    {
                        ConnectionServiceUri = startParams.ConnectionServiceUri.AbsoluteUri,
                    };

                    var env = MockMapper().Map<CloudEnvironment>(envCreateDetails);
                    env.Location = AzureLocation.WestUs2;

                    return env;
                });

            moq
                .Setup(obj => obj.ResumeAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<StartCloudEnvironmentParameters>(),
                    It.IsAny<Subscription>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync((
                    CloudEnvironment env,
                    StartCloudEnvironmentParameters startParams,
                    Subscription subscription,
                    IDiagnosticsLogger logger) =>
                {
                    Assert.Equal(MockServiceUri, startParams.FrontEndServiceUri.ToString());
                    Assert.Equal(MockCallbackUriFormat, startParams.CallbackUriFormat);

                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = env,
                        MessageCode = 0,
                        HttpStatusCode = StatusCodes.Status200OK,
                    };
                });

            moq
                .Setup(obj => obj.UpdateSettingsAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<CloudEnvironmentUpdate>(),
                    It.IsAny<Subscription>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync((
                    CloudEnvironment env,
                    CloudEnvironmentUpdate updateParams,
                    Subscription subscription,
                    IDiagnosticsLogger logger) =>
                {
                    Assert.NotNull(updateParams);

                    return CloudEnvironmentUpdateResult.Success(env);
                });


            if (environment != null)
            {
                moq
                    .Setup(obj => obj.GetAsync(
                        It.Is<Guid>((id) => id.ToString() == environment.Id),
                        It.IsAny<IDiagnosticsLogger>()))
                    .Returns(Task.FromResult(environment));
            }

            return moq.Object;
        }

        public static IPlanManager MockPlanManager(Func<Task<VsoPlan>> getPlan)
        {
            var moq = new Mock<IPlanManager>();

            moq
                .Setup(obj => obj.GetAsync(It.IsAny<VsoPlanInfo>(), It.IsAny<IDiagnosticsLogger>(), It.IsAny<bool>()))
                .Returns(getPlan());

            moq
                .Setup(obj => obj.CheckFeatureFlagsAsync(It.IsAny<VsoPlan>(), It.IsAny<PlanFeatureFlag>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult(true));

            return moq.Object;
        }

        public static HttpContext MockHttpContextFromUri(Uri uri)
        {
            var mockHttpContext = new DefaultHttpContext();
            mockHttpContext.Request.Path = uri.PathAndQuery;
            mockHttpContext.Request.Host = new HostString(uri.Host);
            mockHttpContext.Request.Method = "POST";
            mockHttpContext.Request.Scheme = uri.Scheme;
            mockHttpContext.Request.PathBase = new PathString(string.Empty);
            mockHttpContext.Request.QueryString = new QueryString(string.Empty);

            return mockHttpContext;
        }

        public static async Task<VsoPlan> GeneratePlan(
            string planName = "Test",
            string userId = null,
            AzureLocation location = AzureLocation.WestUs2,
            string subscription = null,
            string resourceGroup = null)
        {
            var model = new VsoPlan
            {
                Plan = new VsoPlanInfo
                {
                    Name = planName,
                    ResourceGroup = resourceGroup ?? "myRG",
                    Subscription = subscription ?? Guid.NewGuid().ToString(),
                    Location = location,
                },
                UserId = userId ?? MockCurrentUserProvider().CurrentUserIdSet.PreferredUserId,
            };

            /*
            var serviceResult = await accountManager.CreateAsync(model, logger);
            Assert.Equal(Plans.Contracts.ErrorCodes.Unknown, serviceResult.ErrorCode);
            Assert.NotNull(serviceResult.VsoPlan);

            return serviceResult.VsoPlan;
            */
            await Task.CompletedTask;
            return model;
        }
    }
}
