using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories.Mocks;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tests
{
    public class EnvironmentManagerBillingTests
    {
        private readonly MockCloudEnvironmentRepository environmentRepository;
        private readonly CloudEnvironmentManager environmentManager;
        private readonly MockBillingEventRepository billingEventRepository;
        private readonly IBillingEventManager billingEventManager;
        private readonly MockClientWorkspaceRepository workspaceRepository;
        private readonly IResourceBrokerResourcesHttpContract resourceBroker;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IDiagnosticsLogger logger;
        private const string testUserId = "test-user";
        private const string testAccessToken = "test-token";
        private const string testCallbackUriFormat = "http://localhost/{0}";
        private static readonly VsoAccountInfo testAccount = new VsoAccountInfo
        {
            Subscription = "00000000-0000-0000-0000-000000000000",
            ResourceGroup = "test-resourcegroup",
            Name = "test-account",
        };

        public EnvironmentManagerBillingTests()
        {
            this.loggerFactory = new DefaultLoggerFactory();
            this.logger = loggerFactory.New();

            this.environmentRepository = new MockCloudEnvironmentRepository();
            this.billingEventRepository = new MockBillingEventRepository();
            this.billingEventManager = new BillingEventManager(this.billingEventRepository);
            this.workspaceRepository = new MockClientWorkspaceRepository();
            this.resourceBroker = new MockResourceBrokerClient();

            this.environmentManager = new CloudEnvironmentManager(
                this.environmentRepository,
                this.resourceBroker,
                this.workspaceRepository,
                this.billingEventManager);
        }

        private async Task<CloudEnvironment> CreateTestEnvironmentAsync(string name = "Test")
        {
            var testEnvironment = await this.environmentManager.CreateEnvironmentAsync(
                new CloudEnvironment
                {
                    FriendlyName = name,
                    Location = AzureLocation.WestUs2,
                    SkuName = "test",
                    AccountId = testAccount.ResourceId,
                },
                new CloudEnvironmentOptions(),
                testCallbackUriFormat,
                testUserId,
                testAccessToken,
                this.logger);
            Assert.NotNull(testEnvironment);
            return testEnvironment;
        }

        private async Task MakeTestEnvironmentAvailableAsync(CloudEnvironment testEnvironment)
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

        private void VerifyEnvironmentStateChange(
            CloudEnvironment environment,
            BillingEvent billingEvent,
            CloudEnvironmentState oldState,
            CloudEnvironmentState newState)
        {
            Assert.Equal(testAccount, billingEvent.Account);
            Assert.Equal(environment.Id, billingEvent.Environment?.Id);
            Assert.Equal(environment.FriendlyName, billingEvent.Environment?.Name);
            Assert.Equal(testUserId, billingEvent.Environment?.UserId);
            Assert.Equal(environment.SkuName, billingEvent.Environment?.Sku?.Name);
            Assert.Equal(BillingEventTypes.EnvironmentStateChange, billingEvent.Type);
            var change = billingEvent.Args as BillingStateChange;
            Assert.NotNull(change);
            Assert.Equal(oldState == default ? null : oldState.ToString(), change.OldValue);
            Assert.Equal(newState == default ? null : newState.ToString(), change.NewValue);
        }

        [Fact]
        public async Task CreateEnvironmentInitializesBillingState()
        {
            var testEnvironment = await CreateTestEnvironmentAsync();

            Assert.Collection(
                this.billingEventRepository.Values.OrderBy(ev => ev.Time),
                (billingEvent) => VerifyEnvironmentStateChange(
                    testEnvironment, billingEvent, default, CloudEnvironmentState.Created),
                (billingEvent) => VerifyEnvironmentStateChange(
                    testEnvironment, billingEvent, CloudEnvironmentState.Created, CloudEnvironmentState.Provisioning));
        }

        [Fact]
        public async Task UpdateEnvironmentUpdatesBillingState()
        {
            var testEnvironment = await CreateTestEnvironmentAsync();
            this.billingEventRepository.Clear();

            await MakeTestEnvironmentAvailableAsync(testEnvironment);

            Assert.Collection(
                this.billingEventRepository.Values,
                (billingEvent) => VerifyEnvironmentStateChange(
                    testEnvironment, billingEvent, CloudEnvironmentState.Provisioning, CloudEnvironmentState.Available));
        }

        [Fact]
        public async Task UnavailableEnvironmentUpdatesBillingState()
        {
            var testEnvironment = await CreateTestEnvironmentAsync();
            await MakeTestEnvironmentAvailableAsync(testEnvironment);
            this.billingEventRepository.Clear();

            // Simulate missing workspace, indicating unavailable connection to cloud environment.
            this.workspaceRepository.MockGetStatus = (workspaceId) => null;

            // The GetEnvironment call should update the environment state when it discovers the missing connection.
            var testEnvironment2 = await this.environmentManager.GetEnvironmentAsync(
                testEnvironment.Id, testUserId, this.logger);

            Assert.Collection(
                this.billingEventRepository.Values,
                (billingEvent) => VerifyEnvironmentStateChange(
                    testEnvironment, billingEvent, CloudEnvironmentState.Available, CloudEnvironmentState.Unavailable));
        }

        [Fact]
        public async Task DeleteEnvironmentUpdatesBillingState()
        {
            var testEnvironment = await CreateTestEnvironmentAsync();
            await MakeTestEnvironmentAvailableAsync(testEnvironment);
            this.billingEventRepository.Clear();

            await this.environmentManager.DeleteEnvironmentAsync(testEnvironment.Id, testUserId, this.logger);

            Assert.Collection(
                this.billingEventRepository.Values,
                (billingEvent) => VerifyEnvironmentStateChange(
                    testEnvironment, billingEvent, CloudEnvironmentState.Available, CloudEnvironmentState.Deleted));
        }
    }
}
