using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Newtonsoft.Json;
using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Test
{
    public class BaseBillingTests
    {
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
            UserId = Guid.NewGuid().ToString(),
            Sku = new Sku { Name = standardLinuxSkuName, Tier = "test" },
        };
        public static readonly EnvironmentBillingInfo testEnvironmentWithPremiumSku = new EnvironmentBillingInfo
        {
            Id = testEnvironment.Id,
            Name = testEnvironment.Name,
            UserId = testEnvironment.UserId,
            Sku = new Sku { Name = premiumLinuxSkuName, Tier = "test" },
        };
        public static readonly EnvironmentBillingInfo testEnvironment2 = new EnvironmentBillingInfo
        {
            Id = Guid.NewGuid().ToString(),
            Name = "testEnvironment2",
            UserId = testEnvironment.UserId,
            Sku = new Sku { Name = standardLinuxSkuName, Tier = "test" },
        };
        public static readonly EnvironmentBillingInfo testEnvironment3 = new EnvironmentBillingInfo
        {
            Id = Guid.NewGuid().ToString(),
            Name = "testEnvironment3",
            UserId = testEnvironment.UserId,
            Sku = new Sku { Name = standardLinuxSkuName, Tier = "test" },
        };
        public readonly IBillingEventRepository repository;
        public readonly IBillingOverrideRepository overrideRepository;
        public readonly PlanManagerSettings planManagerSettings;
        public readonly IPlanRepository planRepository; 
        public readonly BillingEventManager manager;
        public readonly PlanManager planManager;
        public readonly IDiagnosticsLoggerFactory loggerFactory;
        public readonly IDiagnosticsLogger logger;
        public readonly JsonSerializer serializer;

        public BaseBillingTests()
        {
            this.loggerFactory = new DefaultLoggerFactory();
            this.logger = loggerFactory.New();
            this.repository = new MockBillingEventRepository();
            this.overrideRepository = new MockBillingOverrideRepository();
            this.manager = new BillingEventManager(this.repository, this.overrideRepository);
            
            // Setting up the plan manager
            this.planRepository = new MockPlanRepository();
            this.planManagerSettings = new PlanManagerSettings();
            this.planManager = new PlanManager(this.planRepository, this.planManagerSettings);
            
            this.serializer = JsonSerializer.CreateDefault();
        }

    }
}
