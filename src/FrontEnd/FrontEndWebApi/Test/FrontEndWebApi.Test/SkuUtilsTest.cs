using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Utility;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class SkuUtilsTest
    {
        [Fact]
        public async Task IsVisibleAsync()
        {
            var SystemCongiuration = MockUtil.MockSystemConfiguration();
            var logger = MockUtil.MockLogger();
            var skuUtils = new SkuUtils(logger, SystemCongiuration);
            var userProvider = MockUtil.MockCurrentUserProvider();
            var basicSkuFeatureFlag = "basic-sku:is-enabled";
            var sku = MockUtil.MockSku(
                "basicLinux",
                SkuTier.Basic,
                "Basic Linux",
                ComputeOS.Linux,
                2,
                64,
                new ReadOnlyCollection<string>(new string[0]),
                new ReadOnlyCollection<string>(new string[1] { basicSkuFeatureFlag }),
                1);
            var planId = "/subscriptions/8def34ce-053c-43ba-8501-37599fb7f010/resourceGroups/cloudEnvironments/providers/Microsoft.VSOnline/plans/samanoha-dev-stamp-plan";
            var planInfo = VsoPlanInfo.TryParse(planId);

            // with no Sku info.
            var actionResult = await skuUtils.IsVisible(null, planInfo, userProvider.GetProfile());
            Assert.False(actionResult);

            // with no plan info.
            actionResult = await skuUtils.IsVisible(sku, null, userProvider.GetProfile());
            Assert.False(actionResult);

            // with no user profile info. its always true for any non-windows skus as of now.
            actionResult = await skuUtils.IsVisible(sku, planInfo, null);
            Assert.True(actionResult);

            // with all the valid inputs.
            actionResult = await skuUtils.IsVisible(sku, planInfo, userProvider.GetProfile());
            Assert.True(actionResult);
        }
    }
}
