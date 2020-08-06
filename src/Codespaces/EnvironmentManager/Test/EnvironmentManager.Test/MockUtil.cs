using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Profile = Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.Profile;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Tokens;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Test
{
    public static class MockUtil
    {
        public static readonly string MockEnvironmentId = Guid.Empty.ToString().Replace('0', '1');
        public const string MockServiceUri = "https://testhost/test-service-uri/";
        private const string MockCallbackUriFormat = "https://testhost/test-callback-uri/";
        private const string MockUserProviderId = "mock-provider-id";

        public static ISubscriptionManager MockSubscriptionManager()
        {
            var moq = new Mock<ISubscriptionManager>();
            moq.Setup(t => t.CanSubscriptionCreatePlansAndEnvironmentsAsync(It.IsAny<Subscription>(), It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(true);

            moq.Setup(t => t.GetSubscriptionAsync(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), It.IsAny<string>()))
                .ReturnsAsync((string subscriptionId, IDiagnosticsLogger logger, string resourceProvider) =>
                new Subscription
                {
                    Id = subscriptionId,
                    QuotaId = "testQuotaValue",
                    CurrentMaximumQuota = new Dictionary<string, int>
                        {
                            {"computeSkuFamily", 10 },
                        }
                });

            return moq.Object;
        }

        public static IResourceSelectorFactory MockResourceSelectorFactory()
        {
            var moq = new Mock<IResourceSelectorFactory>();

            moq.Setup(x => x.CreateAllocationRequestsAsync(It.IsAny<CloudEnvironment>(), It.IsAny<CloudEnvironmentOptions>(), It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync((CloudEnvironment cloudEnvironment, CloudEnvironmentOptions cloudEnvironmentOptions, IDiagnosticsLogger logger) =>
                {
                    var computeRequest = new AllocateRequestBody
                    {
                        Type = ResourceType.ComputeVM,
                        SkuName = cloudEnvironment.SkuName,
                        Location = cloudEnvironment.Location,
                        QueueCreateResource = false,
                    };

                    var storageRequest = new AllocateRequestBody
                    {
                        Type = ResourceType.StorageFileShare,
                        SkuName = cloudEnvironment.SkuName,
                        Location = cloudEnvironment.Location,
                        QueueCreateResource = false,
                    };

                    return new List<AllocateRequestBody> { computeRequest, storageRequest };
                });

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
                1),
                new CloudEnvironmentSku(
                "windows",
                SkuTier.Standard,
                "Windows SKU Name",
                true,
                new[] { AzureLocation.WestUs2 },
                "computeSkuFamily",
                "computeSkuName",
                "computeSkuSize",
                4,
                ComputeOS.Windows,
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

        public static IPlanManager MockPlanManager(Func<Task<VsoPlan>> getPlan)
        {
            var moq = new Mock<IPlanManager>();

            moq
                .Setup(obj => obj.GetAsync(It.IsAny<VsoPlanInfo>(), It.IsAny<IDiagnosticsLogger>(), It.IsAny<bool>()))
                .Returns(getPlan());

            moq
                .Setup(obj => obj.CheckFeatureFlagsAsync(It.IsAny<VsoPlan>(), It.IsAny<PlanFeatureFlag>(), It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(true);

            return moq.Object;
        }

        public static ITokenProvider MockTokenProvider()
        {
            var moq = new Mock<ITokenProvider>();

            const string issuer = "test-issuer";
            const string audience = "test-audience";

            var key = new SymmetricSecurityKey(JwtTokenUtilities.GenerateKeyBytes(256));
            key.KeyId = Guid.NewGuid().ToString();
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwtWriter = new JwtWriter();
            jwtWriter.AddIssuer(issuer, signingCredentials);
            jwtWriter.AddAudience(audience);

            moq.Setup(obj => obj.IssueTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<IEnumerable<Claim>>(),
                It.IsAny<IDiagnosticsLogger>()))
                .Returns((
                    string issuer,
                    string audience,
                    DateTime expires,
                    IEnumerable<Claim> claims,
                    IDiagnosticsLogger logger)
                    => Task.FromResult(jwtWriter.WriteToken(
                        logger, issuer, audience, expires, claims.ToArray())));

            moq.Setup(obj => obj.Settings)
                .Returns(() => new AuthenticationSettings
                {
                    VsSaaSTokenSettings = new TokenSettings
                    {
                        Issuer = issuer,
                        Audience = audience,
                    },
                    ConnectionTokenSettings = new TokenSettings
                    {
                        Issuer = issuer,
                        Audience = audience,
                    },
                });

            return moq.Object;
        }

        public static Profile MockProfile(
            string provider = "mock-provider",
            string id = "mock-id",
            Dictionary<string, object> programs = null,
            string email = "someone@somewhere.com")
        {
            return new Profile
            {
                Provider = provider,
                Id = id,
                Programs = programs,
                Email = email
            };
        }

        public static IEnvironmentCreateAction MockEnvironmentCreateAction()
        {
            var moq = new Mock<IEnvironmentCreateAction>();
            moq
                .Setup(x => x.RunAsync(
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
                    return Mapper.Map<CloudEnvironment>(envCreateDetails);
                });

            return moq.Object;
        }

        public static IEnvironmentListAction MockEnvironmentListAction()
        {
            var moq = new Mock<IEnvironmentListAction>();
            moq
                .Setup(x => x.RunAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<VsoClaimsIdentity>(),
                    It.IsAny<UserIdSet>(),
                    It.IsAny<EnvironmentListType>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync((
                    string planId,
                    string name,
                    VsoClaimsIdentity identity,
                    UserIdSet userIdSet,
                    EnvironmentListType environmentListType,
                    IDiagnosticsLogger logger) =>
                {
                    return Enumerable.Empty<CloudEnvironment>();
                });

            return moq.Object;
        }

        public static IEnvironmentGetAction MockEnvironmentGetAction()
        {
            var moq = new Mock<IEnvironmentGetAction>();
            moq
                .Setup(x => x.RunAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync((
                    Guid id,
                    IDiagnosticsLogger logger) =>
                {
                    return new CloudEnvironment();
                });

            return moq.Object;
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

            await Task.CompletedTask;
            return model;
        }
    }
}
