using System;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Accounts.Tests
{
    public class AccountInfoTests
    {
        [Fact]
        public void AccountResourceId()
        {
            var account = new VsoAccountInfo
            {
                Subscription = Guid.Empty.ToString(),
                ResourceGroup = "testRG",
                Name = "testA",
            };

            string resId = $"/subscriptions/{Guid.Empty}/resourceGroups/testRG/providers/{VsoAccountInfo.ProviderName}/{VsoAccountInfo.AccountResourceType}/testA";
            Assert.Equal(resId, account.ResourceId);
        }

        [Fact]
        public void AccountResourceIdInvalidSubscription()
        {
            var account = new VsoAccountInfo
            {
                Subscription = "invalid",
                ResourceGroup = "testRG",
                Name = "testA",
            };

            Assert.Throws<ArgumentException>(() => account.ResourceId);
        }

        [Fact]
        public void ParseAccountId()
        {
            string resId = $"/subscriptions/{Guid.Empty}/resourceGroups/testRG/providers/{VsoAccountInfo.ProviderName}/{VsoAccountInfo.AccountResourceType}/testA";
            Assert.True(VsoAccountInfo.TryParse(resId, out var account));
            Assert.Equal(Guid.Empty.ToString(), account.Subscription);
            Assert.Equal("testRG", account.ResourceGroup);
            Assert.Equal("testA", account.Name);
        }

        [Fact]
        public void ParseAccountInvalid()
        {
            string invalidResId = $"/subscriptions/{Guid.Empty}/resourceGroups/testRG/providers/{VsoAccountInfo.ProviderName}/testA";
            Assert.False(VsoAccountInfo.TryParse(invalidResId, out _));
        }

        [Fact]
        public void ParseAccountInvalidSubscription()
        {
            string invalidResId = $"/subscriptions/1234/resourceGroups/testRG/{VsoAccountInfo.ProviderName}/{VsoAccountInfo.AccountResourceType}/testA";
            Assert.False(VsoAccountInfo.TryParse(invalidResId, out _));
        }

        [Fact]
        public void ParseAccountInvalidName()
        {
            string invalidResId = $"/subscriptions/{Guid.Empty}/resourceGroups/testRG/{VsoAccountInfo.ProviderName}/{VsoAccountInfo.AccountResourceType}/testA?api-version=1";
            Assert.False(VsoAccountInfo.TryParse(invalidResId, out _));
        }
    }
}
