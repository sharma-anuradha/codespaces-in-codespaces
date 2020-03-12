using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Utility;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;
using Profile = Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Profile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public static class MockUtil
    {
        public const string MockServiceUri = "https://testhost/test-service-uri/";
        private const string MockCallbackUriFormat = "https://testhost/test-callback-uri/";
        private const string MockUserProviderId = "mock-provider-id";

        public static CloudEnvironment MockCloudEnvironment(
            string ownerId = null,
            string planId = null)
        {
            return new CloudEnvironment
            {
                Id = Guid.NewGuid().ToString(),
                Location = AzureLocation.WestUs2,
                OwnerId = ownerId ?? MockCurrentUserProvider().GetCurrentUserIdSet().PreferredUserId,
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

        public static ISkuUtils MockSkuUtils(bool value)
        {
            var moq = new Mock<ISkuUtils>();
             moq.Setup(x => x.IsVisible(It.IsAny<CloudEnvironmentSku>(), It.IsAny<VsoPlanInfo>(), It.IsAny<Profile>()))
                .Returns((CloudEnvironmentSku sku, VsoPlanInfo planInfo, UserProfile.Profile profile) => Task.FromResult(value));
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
            IEnumerable<string> supportedFeatures)
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
                new ReadOnlyCollection<string>(skuTransitions.ToList()),
                new ReadOnlyCollection<string>(supportedFeatures.ToList()));
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
                2.0m,
                125.0m,
                5,
                5,
                new ReadOnlyCollection<string>(new string[0]),
                new ReadOnlyCollection<string>(new string[0])));
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
            Dictionary<string, object> programs = null, string email = default)
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
                        ProviderId = MockUserProviderId,
                        Programs = programs,
                        Email = email
                    };
                });

            return moq.Object;
        }

        public static IEnvironmentManager MockEnvironmentManager(CloudEnvironment environment = null)
        {
            var moq = new Mock<IEnvironmentManager>();

            moq
                .Setup(obj => obj.CreateAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<CloudEnvironmentOptions>(),
                    It.IsAny<StartCloudEnvironmentParameters>(),
                    It.IsAny<VsoPlanInfo>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync((
                    CloudEnvironment env,
                    CloudEnvironmentOptions options,
                    StartCloudEnvironmentParameters startParams,
                    VsoPlanInfo plan,
                    IDiagnosticsLogger logger) =>
                {
                    Assert.Equal(MockServiceUri, startParams.FrontEndServiceUri.ToString());
                    Assert.Equal(MockCallbackUriFormat, startParams.CallbackUriFormat);

                    env.Connection = new EnvironmentManager.ConnectionInfo
                    {
                        ConnectionServiceUri = startParams.ConnectionServiceUri.AbsoluteUri,
                    };

                    return new CloudEnvironmentServiceResult
                    {
                        CloudEnvironment = env,
                        MessageCode = 0,
                        HttpStatusCode = StatusCodes.Status200OK,
                    };
                });

            moq
                .Setup(obj => obj.ResumeAsync(
                    It.IsAny<CloudEnvironment>(),
                    It.IsAny<StartCloudEnvironmentParameters>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync((
                    CloudEnvironment env,
                    StartCloudEnvironmentParameters startParams,
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

            if (environment != null)
            {
                moq
                    .Setup(obj => obj.GetAndStateRefreshAsync(
                        It.Is<string>((id) => id == environment.Id),
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
            AzureLocation location = AzureLocation.WestUs2)
        {
            var model = new VsoPlan
            {
                Plan = new VsoPlanInfo
                {
                    Name = planName,
                    ResourceGroup = "myRG",
                    Subscription = Guid.NewGuid().ToString(),
                    Location = location,
                },
                UserId = userId ?? MockCurrentUserProvider().GetCurrentUserIdSet().PreferredUserId,
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
