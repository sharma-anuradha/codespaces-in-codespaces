using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class ProfileUtilTests
    {
        [Fact]
        public void InternalWindowsSkuVisibility()
        {
            var msProfile = new Profile
            {
                Email = "coder@microsoft.com"
            };
            Assert.True(msProfile.IsWindowsSkuInternalUser());

            var ms2Profile = new Profile
            {
                Email = "coder@int.microsoft.com"
            };
            Assert.True(ms2Profile.IsWindowsSkuInternalUser());

            var outsideProfile = new Profile
            {
                Email = "person@gmail.com"
            };
            Assert.False(outsideProfile.IsWindowsSkuInternalUser());

            var fakeProfile = new Profile
            {
                Email = "person@fakemicrosoft.com"
            };
            Assert.False(fakeProfile.IsWindowsSkuInternalUser());

            
        }
    }
}
