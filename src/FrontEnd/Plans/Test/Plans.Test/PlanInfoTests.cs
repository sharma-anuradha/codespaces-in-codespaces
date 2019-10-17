using System;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Tests
{
    public class PlanInfoTests
    {
        [Fact]
        public void PlanResourceId()
        {
            var plan = new VsoPlanInfo
            {
                Subscription = Guid.Empty.ToString(),
                ResourceGroup = "testRG",
                Name = "testA",
            };

            string resId = $"/subscriptions/{Guid.Empty}/resourceGroups/testRG/providers/{VsoPlanInfo.ProviderName}/{VsoPlanInfo.PlanResourceType}/testA";
            Assert.Equal(resId, plan.ResourceId);
        }

        [Fact]
        public void PlanResourceIdInvalidSubscription()
        {
            var plan = new VsoPlanInfo
            {
                Subscription = "invalid",
                ResourceGroup = "testRG",
                Name = "testA",
            };

            Assert.Throws<ArgumentException>(() => plan.ResourceId);
        }

        [Fact]
        public void ParsePlanId()
        {
            string resId = $"/subscriptions/{Guid.Empty}/resourceGroups/testRG/providers/{VsoPlanInfo.ProviderName}/{VsoPlanInfo.PlanResourceType}/testA";
            Assert.True(VsoPlanInfo.TryParse(resId, out var plan));
            Assert.Equal(Guid.Empty.ToString(), plan.Subscription);
            Assert.Equal("testRG", plan.ResourceGroup);
            Assert.Equal("testA", plan.Name);
        }

        [Fact]
        public void ParsePlanInvalid()
        {
            string invalidResId = $"/subscriptions/{Guid.Empty}/resourceGroups/testRG/providers/{VsoPlanInfo.ProviderName}/testA";
            Assert.False(VsoPlanInfo.TryParse(invalidResId, out _));
        }

        [Fact]
        public void ParsePlanInvalidSubscription()
        {
            string invalidResId = $"/subscriptions/1234/resourceGroups/testRG/{VsoPlanInfo.ProviderName}/{VsoPlanInfo.PlanResourceType}/testA";
            Assert.False(VsoPlanInfo.TryParse(invalidResId, out _));
        }

        [Fact]
        public void ParsePlanInvalidName()
        {
            string invalidResId = $"/subscriptions/{Guid.Empty}/resourceGroups/testRG/{VsoPlanInfo.ProviderName}/{VsoPlanInfo.PlanResourceType}/testA?api-version=1";
            Assert.False(VsoPlanInfo.TryParse(invalidResId, out _));
        }
    }
}
