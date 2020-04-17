using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Mocks;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Test
{
    public class BaseBillingTests
    {
        private readonly decimal standardLinuxComputeUnitPerHr = 125;
        private readonly decimal premiumLinuxComputeUnitPerHr = 242;
        private readonly decimal standardLinuxStorageUnitPerHr = 2;
        private readonly decimal premiumLinuxStorageUnitPerHr = 3;

        public static readonly string subscription = Guid.NewGuid().ToString();
        public static readonly string standardLinuxSkuName = "standardLinuxSku";
        public static readonly string premiumLinuxSkuName = "premiumLinuxSku";
        public static readonly VsoPlanInfo testPlan = new VsoPlanInfo
        {
            Subscription = subscription,
            ResourceGroup = "testRG",
            Name = "testPlan",
            Location = AzureLocation.WestUs2,
        };
        public static readonly VsoPlanInfo testPlan2 = new VsoPlanInfo
        {
            Subscription = subscription,
            ResourceGroup = "testRG",
            Name = "testPlan2",
            Location = AzureLocation.WestUs2,
        };
        public static readonly VsoPlanInfo testPlan3 = new VsoPlanInfo
        {
            Subscription = subscription,
            ResourceGroup = "testRG",
            Name = "testPlan3",
            Location = AzureLocation.WestUs2,
        };
        public static readonly EnvironmentBillingInfo testEnvironment = new EnvironmentBillingInfo
        {
            Id = Guid.NewGuid().ToString(),
            Name = "testEnvironment",
            Sku = new Sku { Name = standardLinuxSkuName, Tier = "test" },
        };
        public static readonly EnvironmentBillingInfo testEnvironmentWithPremiumSku = new EnvironmentBillingInfo
        {
            Id = testEnvironment.Id,
            Name = testEnvironment.Name,
            Sku = new Sku { Name = premiumLinuxSkuName, Tier = "test" },
        };
        public static readonly EnvironmentBillingInfo testEnvironment2 = new EnvironmentBillingInfo
        {
            Id = Guid.NewGuid().ToString(),
            Name = "testEnvironment2",
            Sku = new Sku { Name = standardLinuxSkuName, Tier = "test" },
        };
        public static readonly EnvironmentBillingInfo testEnvironment3 = new EnvironmentBillingInfo
        {
            Id = Guid.NewGuid().ToString(),
            Name = "testEnvironment3",
            Sku = new Sku { Name = standardLinuxSkuName, Tier = "test" },
        };
        public readonly IBillingEventRepository repository;
        public readonly IBillingOverrideRepository overrideRepository;
        public readonly PlanManagerSettings planManagerSettings;
        public readonly IPlanRepository planRepository;
        public readonly ISubscriptionManager subscriptionManager;
        public readonly BillingEventManager manager;
        public readonly PlanManager planManager;
        public readonly IDiagnosticsLoggerFactory loggerFactory;
        public readonly IDiagnosticsLogger logger;
        public readonly JsonSerializer serializer;

        public BaseBillingTests()
        {
            loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();
            repository = new MockBillingEventRepository();
            overrideRepository = new MockBillingOverrideRepository();
            manager = new BillingEventManager(repository, overrideRepository);
            
            // Setting up the plan manager
            planRepository = new MockPlanRepository();
            planManagerSettings = new PlanManagerSettings();
            subscriptionManager = new MockSubscriptionManager();
            planManager = new PlanManager(planRepository, planManagerSettings, GetMockSKuCatalog().Object, subscriptionManager);
            
            serializer = JsonSerializer.CreateDefault();
        }

        protected Mock<ISkuCatalog> GetMockSKuCatalog()
        {
            var mockStandardLinux = new Mock<ICloudEnvironmentSku>();
            mockStandardLinux.Setup(sku => sku.ComputeVsoUnitsPerHour).Returns(standardLinuxComputeUnitPerHr);
            mockStandardLinux.Setup(sku => sku.StorageVsoUnitsPerHour).Returns(standardLinuxStorageUnitPerHr);

            var mockPremiumLinux = new Mock<ICloudEnvironmentSku>();
            mockPremiumLinux.Setup(sku => sku.ComputeVsoUnitsPerHour).Returns(premiumLinuxComputeUnitPerHr);
            mockPremiumLinux.Setup(sku => sku.StorageVsoUnitsPerHour).Returns(premiumLinuxStorageUnitPerHr);

            var skus = new Dictionary<string, ICloudEnvironmentSku>
            {
                [standardLinuxSkuName] = mockStandardLinux.Object,
                [premiumLinuxSkuName] = mockPremiumLinux.Object,
            };
            var mockSkuCatelog = new Mock<ISkuCatalog>();
            mockSkuCatelog.Setup(cat => cat.CloudEnvironmentSkus).Returns(skus);
            return mockSkuCatelog;
        }
    }
}
