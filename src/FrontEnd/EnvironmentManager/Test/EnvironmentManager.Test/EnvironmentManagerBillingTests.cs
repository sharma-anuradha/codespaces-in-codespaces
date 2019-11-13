using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Test;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tests
{
    public class EnvironmentManagerBillingTests : EnvironmentManagerTestsBase
    {
        private void VerifyEnvironmentStateChange(
            CloudEnvironment environment,
            BillingEvent billingEvent,
            CloudEnvironmentState oldState,
            CloudEnvironmentState newState)
        {
            Assert.Equal(testPlan, billingEvent.Plan);
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
            await this.planRepository.CreateAsync(testVsoPlan, this.logger);

            var testEnvironment = (await CreateTestEnvironmentAsync()).CloudEnvironment;

            Assert.Collection(
                this.billingEventRepository.Values,
                (billingEvent) => VerifyEnvironmentStateChange(
                    testEnvironment, billingEvent, CloudEnvironmentState.Created, CloudEnvironmentState.Provisioning));
        }

        [Fact]
        public async Task UpdateEnvironmentUpdatesBillingState()
        {
            await this.planRepository.CreateAsync(testVsoPlan, this.logger);
            var testEnvironment = (await CreateTestEnvironmentAsync()).CloudEnvironment;
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
            await this.planRepository.CreateAsync(testVsoPlan, this.logger);
            var testEnvironment = (await CreateTestEnvironmentAsync()).CloudEnvironment;
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
            await this.planRepository.CreateAsync(testVsoPlan, this.logger);
            var testEnvironment = (await CreateTestEnvironmentAsync()).CloudEnvironment;
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
