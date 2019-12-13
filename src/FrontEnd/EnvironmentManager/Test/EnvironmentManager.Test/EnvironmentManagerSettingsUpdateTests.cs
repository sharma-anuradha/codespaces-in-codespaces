﻿using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveshareAuthentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VsSaaS.Common;
using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Test
{
    public class EnvironmentManagerSettingsUpdateTests
    {
        private static readonly IDiagnosticsLogger Logger = new DefaultLoggerFactory().New();

        [Fact]
        public void EnvironmentSettingsUpdate_GetEnvironmentAvailableSettingsUpdates()
        {
            var sku1 = MockSku(skuName: "Sku1", skuTransitions: new[] { "Sku2" });
            var sku2 = MockSku(skuName: "Sku2");

            var skuCatalog = MockSkuCatalog(sku1, sku2);

            var environment = new CloudEnvironment()
            {
                Location = AzureLocation.WestUs2,
                SkuName = sku1.SkuName,
                Id = Guid.NewGuid().ToString(),
            };

            var expectedAutoShutdownOptions = new[] { 123, 456, };
            var manager = CreateManager(skuCatalog: skuCatalog, autoShutdownDelayOptions: expectedAutoShutdownOptions);

            var result = manager.GetEnvironmentAvailableSettingsUpdates(environment, MockProfile(), Logger);

            Assert.Equal(expectedAutoShutdownOptions, result.AllowedAutoShutdownDelayMinutes);
            Assert.Single(result.AllowedSkus);
            Assert.Equal(sku2, result.AllowedSkus[0]);
        }

        [Fact]
        public async Task EnvironmentSettingsUpdate_UpdateEnironmentSettings()
        {
            var targetSku = MockSku(skuName: "TargetSku");
            var activeSku = MockSku(skuName: "ActiveSku", skuTransitions: new[] { targetSku.SkuName });
            
            var skuCatalog = MockSkuCatalog(targetSku, activeSku);

            var currentUserProvider = MockCurrentUserProvider();

            var environmentRepository = new MockCloudEnvironmentRepository();
            
            var environment = MockEnvironment(
                skuName: activeSku.SkuName, 
                ownerId: currentUserProvider.GetProfileId(), 
                state: CloudEnvironmentState.Shutdown,
                autoShutdownDelayMinutes: 0);

            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
                AutoShutdownDelayMinutes = 30,
                SkuName = targetSku.SkuName,
            };

            var manager = CreateManager(environmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateEnvironmentSettingsAsync(environment.Id, update, currentUserProvider, Logger);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.CloudEnvironment);
            Assert.Equal(update.AutoShutdownDelayMinutes, result.CloudEnvironment.AutoShutdownDelayMinutes);
            Assert.Equal(update.SkuName, result.CloudEnvironment.SkuName);
        }

        [Fact]
        public async Task EnvironmentSettingsUpdate_UpdateEnironmentSettings_NotFound()
        {
            var environmentRepository = new Mock<ICloudEnvironmentRepository>();
            environmentRepository
                .Setup(x => x.GetAsync(It.IsAny<DocumentDbKey>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((CloudEnvironment)null));

            var update = new CloudEnvironmentUpdate
            {
            };

            var manager = CreateManager(environmentRepository: environmentRepository.Object);

            var result = await manager.UpdateEnvironmentSettingsAsync("id", update, MockCurrentUserProvider(), Logger);

            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(Contracts.ErrorCodes.EnvironmentDoesNotExist, result.ValidationErrors.First());
        }

        [Fact]
        public async Task EnvironmentSettingsUpdate_UpdateEnironmentSettings_NotShutdown()
        {
            var sku = MockSku();
            var skuCatalog = MockSkuCatalog(sku);

            var currentUserProvider = MockCurrentUserProvider();

            var environmentRepository = new MockCloudEnvironmentRepository();

            var environment = MockEnvironment(skuName: sku.SkuName, ownerId: currentUserProvider.GetProfileId(), state: CloudEnvironmentState.Available);
            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
            };

            var manager = CreateManager(environmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateEnvironmentSettingsAsync(environment.Id, update, currentUserProvider, Logger);

            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(Contracts.ErrorCodes.EnvironmentNotShutdown, result.ValidationErrors.First());
        }

        [Fact]
        public async Task EnvironmentSettingsUpdate_UpdateEnironmentSettings_InvalidAutoShutdown()
        {
            var sku = MockSku();

            var skuCatalog = MockSkuCatalog(sku);

            var currentUserProvider = MockCurrentUserProvider();

            var environmentRepository = new MockCloudEnvironmentRepository();

            var environment = MockEnvironment(
                skuName: sku.SkuName,
                ownerId: currentUserProvider.GetProfileId(),
                state: CloudEnvironmentState.Shutdown);

            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
                AutoShutdownDelayMinutes = -1,
            };

            var manager = CreateManager(environmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateEnvironmentSettingsAsync(environment.Id, update, currentUserProvider, Logger);

            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(Contracts.ErrorCodes.RequestedAutoShutdownDelayMinutesIsInvalid, result.ValidationErrors.First());
        }

        [Fact]
        public async Task EnvironmentSettingsUpdate_UpdateEnironmentSettings_SkuUpdateNotAllowed()
        {
            var sku = MockSku();

            var skuCatalog = MockSkuCatalog(sku);

            var currentUserProvider = MockCurrentUserProvider();

            var environmentRepository = new MockCloudEnvironmentRepository();

            var environment = MockEnvironment(
                skuName: sku.SkuName,
                ownerId: currentUserProvider.GetProfileId(),
                state: CloudEnvironmentState.Shutdown);

            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
                SkuName = "some sku",
            };

            var manager = CreateManager(environmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateEnvironmentSettingsAsync(environment.Id, update, currentUserProvider, Logger);

            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(Contracts.ErrorCodes.UnableToUpdateSku, result.ValidationErrors.First());
        }

        [Fact]
        public async Task EnvironmentSettingsUpdate_UpdateEnironmentSettings_InvalidSku()
        {
            var targetSku = MockSku(skuName: "TargetSku");
            var activeSku = MockSku(skuName: "ActiveSku", skuTransitions: new[] { targetSku.SkuName });

            var skuCatalog = MockSkuCatalog(targetSku, activeSku);

            var currentUserProvider = MockCurrentUserProvider();

            var environmentRepository = new MockCloudEnvironmentRepository();

            var environment = MockEnvironment(
                skuName: activeSku.SkuName,
                ownerId: currentUserProvider.GetProfileId(),
                state: CloudEnvironmentState.Shutdown);

            environment = await environmentRepository.CreateAsync(environment, Logger);

            var update = new CloudEnvironmentUpdate
            {
                SkuName = "bad sku name",
            };

            var manager = CreateManager(environmentRepository: environmentRepository, skuCatalog: skuCatalog);

            var result = await manager.UpdateEnvironmentSettingsAsync(environment.Id, update, currentUserProvider, Logger);

            Assert.False(result.IsSuccess);
            Assert.Single(result.ValidationErrors);
            Assert.Equal(Contracts.ErrorCodes.RequestedSkuIsInvalid, result.ValidationErrors.First());
        }

        private CloudEnvironmentManager CreateManager(
            ICloudEnvironmentRepository environmentRepository = null,
            ISkuCatalog skuCatalog = null,
            int[] autoShutdownDelayOptions = null)
        {
            var defaultCount = 20;
            var defaultAutoShutdownOptions = new[] { 0, 5, 30, 120 };
            var planSettings = new PlanManagerSettings() { DefaultMaxPlansPerSubscription = defaultCount };
            var environmentSettings = new EnvironmentManagerSettings() 
            {
                DefaultMaxEnvironmentsPerPlan = defaultCount,
                DefaultAutoShutdownDelayMinutesOptions = autoShutdownDelayOptions ?? defaultAutoShutdownOptions,
            };

            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<int>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), defaultCount))
                .Returns(Task.FromResult(defaultCount));

            planSettings.Init(mockSystemConfiguration.Object);
            environmentSettings.Init(mockSystemConfiguration.Object);

            environmentRepository = environmentRepository ?? new MockCloudEnvironmentRepository();
            var planRepository = new MockPlanRepository();
            var billingEventRepository = new MockBillingEventRepository();
            var accountManager = new PlanManager(planRepository, planSettings);
            var billingEventManager = new BillingEventManager(billingEventRepository, new MockBillingOverrideRepository());
            var workspaceRepository = new MockClientWorkspaceRepository();
            var authRepository = new MockClientAuthRepository();
            var resourceBroker = new MockResourceBrokerClient();

            skuCatalog = skuCatalog ?? MockSkuCatalog();

            return new CloudEnvironmentManager(
                environmentRepository,
                resourceBroker,
                workspaceRepository,
                accountManager,
                authRepository,
                billingEventManager,
                skuCatalog,
                environmentSettings);
        }

        private static ICloudEnvironmentSku MockSku(
            string skuName = "MockSkuName",
            string displayName = "MockSkuDisplayName",
            ComputeOS computeOs = ComputeOS.Linux,
            decimal storageUnits = 2,
            decimal computeUnits = 125,
            IEnumerable<string> skuTransitions = null)
        {
            skuTransitions = skuTransitions ?? new List<string>();

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
                    MockControlPlaneStampInfo(),
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

        private static ISkuCatalog MockSkuCatalog(params ICloudEnvironmentSku[] skus)
        {
            var skuDict = new ReadOnlyDictionary<string, ICloudEnvironmentSku>(skus.ToDictionary((s) => s.SkuName));

            var moq = new Mock<ISkuCatalog>();
            moq
                .Setup(obj => obj.CloudEnvironmentSkus)
                .Returns(skuDict);

            return moq.Object;
        }

        private static IControlPlaneStampInfo MockControlPlaneStampInfo()
        {            
            return new Mock<IControlPlaneStampInfo>().Object;
        }

        private ICurrentUserProvider MockCurrentUserProvider(
            string profileId = "mock-profile-id",
            string bearerToken = "mock-bearer-token",
            Profile currentUser = null)
        {
            currentUser = currentUser ?? MockProfile();

            var moq = new Mock<ICurrentUserProvider>();
            moq
                .Setup(obj => obj.GetProfileId())
                .Returns(profileId);
            moq
                .Setup(obj => obj.GetBearerToken())
                .Returns(bearerToken);
            moq
                .Setup(obj => obj.GetProfile())
                .Returns(currentUser);

            return moq.Object;
        }

        private Profile MockProfile(Dictionary<string, object> programs = null)
        {
            return new Profile
            {
                ProviderId = "mock-provider-id",
                Programs = programs
            };
        }

        public CloudEnvironment MockEnvironment(
            string name = "env-name",
            string id = "env-id",
            AzureLocation location = AzureLocation.WestUs2,
            string planId = null,
            string ownerId = "owner-id",
            string skuName = "sku-name",
            CloudEnvironmentState state = CloudEnvironmentState.Available,
            int autoShutdownDelayMinutes = 0)
        {
            return new CloudEnvironment
            {
                Id = id,
                FriendlyName = name,
                Location = location,
                SkuName = skuName,
                PlanId = planId ?? MockPlan().ResourceId,
                OwnerId = ownerId,
                State = state,
                AutoShutdownDelayMinutes = autoShutdownDelayMinutes
            };
        }

        public VsoPlanInfo MockPlan(
            string subscription = "00000000-0000-0000-0000-000000000000",
            string resourceGroup = "test-resourcegroup",
            string name = "test-plan",
            AzureLocation location = AzureLocation.WestUs2)
        {
            return new VsoPlanInfo
            {
                Subscription = subscription,
                ResourceGroup = resourceGroup,
                Name = name,
                Location = location,
            };
        }
    }
}
