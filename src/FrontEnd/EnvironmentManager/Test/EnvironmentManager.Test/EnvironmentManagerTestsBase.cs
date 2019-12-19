﻿using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveshareAuthentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Moq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Test
{
    public class EnvironmentManagerTestsBase
    {
        public readonly MockCloudEnvironmentRepository environmentRepository;
        public readonly MockPlanRepository planRepository;
        public readonly MockBillingEventRepository billingEventRepository;
        public readonly MockClientWorkspaceRepository workspaceRepository;
        public readonly IPlanManager accountManager;
        public readonly IBillingEventManager billingEventManager;
        public readonly MockClientAuthRepository authRepository;
        public readonly IResourceBrokerResourcesHttpContract resourceBroker;
        public readonly CloudEnvironmentManager environmentManager;
        public readonly IDiagnosticsLoggerFactory loggerFactory;
        public readonly IDiagnosticsLogger logger;
        public readonly ISkuCatalog skuCatalog;
        public const string testUserId = "test-user";
        public const string testAccessToken = "test-token";
        public readonly Uri testServiceUri = new Uri("http://localhost/");
        public const string testCallbackUriFormat = "http://localhost/{0}";

        public static readonly VsoPlanInfo testPlan = new VsoPlanInfo
        {
            Subscription = "00000000-0000-0000-0000-000000000000",
            ResourceGroup = "test-resourcegroup",
            Name = "test-plan",
            Location = AzureLocation.WestUs2
        };
        public static readonly VsoPlan testVsoPlan = new VsoPlan
        {
            Id = "11100000-0000-0000-0000-000000000000",
            Plan = testPlan,
        };

        public EnvironmentManagerTestsBase()
        {
            this.loggerFactory = new DefaultLoggerFactory();
            this.logger = loggerFactory.New();

            var defaultCount = 20;
            var planSettings = new PlanManagerSettings() { DefaultMaxPlansPerSubscription = defaultCount };
            var environmentSettings = new EnvironmentManagerSettings() { DefaultMaxEnvironmentsPerPlan = defaultCount };

            var mockSystemConfiguration = new Mock<ISystemConfiguration>();
            mockSystemConfiguration
                .Setup(x => x.GetValueAsync<int>(It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>(), defaultCount))
                .Returns(Task.FromResult(defaultCount));

            planSettings.Init(mockSystemConfiguration.Object);
            environmentSettings.Init(mockSystemConfiguration.Object);

            this.environmentRepository = new MockCloudEnvironmentRepository();
            this.planRepository = new MockPlanRepository();
            this.billingEventRepository = new MockBillingEventRepository();
            this.accountManager = new PlanManager(this.planRepository, planSettings);
            this.billingEventManager = new BillingEventManager(this.billingEventRepository,
                                                                new MockBillingOverrideRepository());
            this.workspaceRepository = new MockClientWorkspaceRepository();
            this.authRepository = new MockClientAuthRepository();
            this.resourceBroker = new MockResourceBrokerClient();

            this.skuCatalog = new Mock<ISkuCatalog>(MockBehavior.Strict).Object;

            this.environmentManager = new CloudEnvironmentManager(
                this.environmentRepository,
                this.resourceBroker,
                this.workspaceRepository,
                this.accountManager,
                this.authRepository,
                this.billingEventManager,
                this.skuCatalog,
                environmentSettings);
        }

        public async Task<CloudEnvironmentServiceResult> CreateTestEnvironmentAsync(string name = "Test")
        {
            var serviceResult = await this.environmentManager.CreateEnvironmentAsync(
                new CloudEnvironment
                {
                    FriendlyName = name,
                    Location = AzureLocation.WestUs2,
                    SkuName = "test",
                    PlanId = testPlan.ResourceId,
                    OwnerId = testUserId
                },
                new CloudEnvironmentOptions(),
                testServiceUri,
                testCallbackUriFormat,
                testUserId,
                testUserId,
                testAccessToken,
                this.logger);
            
            return serviceResult;
        }

        public async Task MakeTestEnvironmentAvailableAsync(CloudEnvironment testEnvironment)
        {
            await this.environmentManager.UpdateEnvironmentCallbackAsync(
                testEnvironment.Id,
                new EnvironmentRegistrationCallbackOptions
                {
                    Payload = new EnvironmentRegistrationCallbackPayloadOptions
                    {
                        SessionId = testEnvironment.Connection?.ConnectionSessionId,
                        SessionPath = "/",
                    },
                },
                testUserId,
                this.logger);
        }
    }
}
