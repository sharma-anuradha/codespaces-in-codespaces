using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Test
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
            await planRepository.CreateAsync(testVsoPlan, logger);

            var testEnvironment = (await CreateTestEnvironmentAsync());

            Assert.Collection(
                billingEventRepository.Values.OrderBy(item => item.Time),
                (billingEvent) => VerifyEnvironmentStateChange(
                        testEnvironment, billingEvent, CloudEnvironmentState.Created, CloudEnvironmentState.Created),
                (billingEvent) => VerifyEnvironmentStateChange(
                    testEnvironment, billingEvent, CloudEnvironmentState.Created, CloudEnvironmentState.Provisioning));
        }

        [Fact]
        public async Task UpdateEnvironmentUpdatesBillingState()
        {
            await planRepository.CreateAsync(testVsoPlan, logger);
            var testEnvironment = (await CreateTestEnvironmentAsync());
            billingEventRepository.Clear();

            await MakeTestEnvironmentAvailableAsync(testEnvironment);

            Assert.Collection(
                billingEventRepository.Values,
                (billingEvent) => VerifyEnvironmentStateChange(
                    testEnvironment, billingEvent, CloudEnvironmentState.Provisioning, CloudEnvironmentState.Available));
        }

        [Fact]
        public async Task UnavailableEnvironmentUpdatesBillingState()
        {
            // TODO: elpadann - This check is moved down to the controller. Rewrite this test in controller tests.
            /*
            await planRepository.CreateAsync(testVsoPlan, logger);
            var testEnvironment = (await CreateTestEnvironmentAsync());
            await MakeTestEnvironmentAvailableAsync(testEnvironment);
            billingEventRepository.Clear();

            // Simulate missing workspace, indicating unavailable connection to cloud environment.
            workspaceRepository.MockGetStatus = (workspaceId) => null;

            // The GetEnvironment call should update the environment state when it discovers the missing connection.
            await this.environmentManager.GetAsync(Guid.Parse(testEnvironment.Id), this.logger);

            Assert.Collection(
                 billingEventRepository.Values,
                 (billingEvent) => VerifyEnvironmentStateChange(
                     testEnvironment, billingEvent, CloudEnvironmentState.Available, CloudEnvironmentState.Unavailable));
            */

            await Task.CompletedTask;
        }

        [Fact]
        public async Task DeleteEnvironmentUpdatesBillingState()
        {
            await planRepository.CreateAsync(testVsoPlan, logger);
            var testEnvironment = (await CreateTestEnvironmentAsync());
            await MakeTestEnvironmentAvailableAsync(testEnvironment);
            billingEventRepository.Clear();

            await this.environmentManager.DeleteAsync(testEnvironment, this.logger);

            Assert.Collection(
                billingEventRepository.Values,
                (billingEvent) => VerifyEnvironmentStateChange(
                    testEnvironment, billingEvent, CloudEnvironmentState.Available, CloudEnvironmentState.Deleted));
        }
    }
}
