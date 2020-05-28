using System;
using Xunit;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Tests
{
    public class SubnetResourceIdTests
    {
        private readonly IDiagnosticsLogger logger;
      
        public SubnetResourceIdTests()
        {
            var loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();
        }

        [Fact]
        public void SubnetResourceIdValid()
        {
            var resourceId = $"/subscriptions/{Guid.NewGuid()}/resourceGroups/VSEng/providers/Microsoft.Network/virtualNetworks/VSEng-vnet/subnets/default";
            Assert.True(resourceId.IsValidSubnetResourceId(logger));
        }

        [Fact]
        public void SubnetResourceIdEmptyResourceId()
        {
            Assert.False(string.Empty.IsValidSubnetResourceId(logger));
        }

        [Fact]
        public void SubnetResourceIdInvalidResourceId()
        {
            Assert.False("somestring".IsValidSubnetResourceId(logger));
        }

        [Fact]
        public void SubnetResourceIdInvalidSubscription()
        {
            var resourceId = GetResourceId("invalid");
            Assert.False(resourceId.IsValidSubnetResourceId(logger));
        }

        [Fact]
        public void SubnetResourceIdEmptyGuidSubscription()
        {
            var resourceId = GetResourceId(Guid.Empty.ToString());
            Assert.True(resourceId.IsValidSubnetResourceId(logger));
        }

        private static string GetResourceId(string subscriptionId)
        {
            return $"/subscriptions/{subscriptionId}/resourceGroups/VSEng/providers/Microsoft.Network/virtualNetworks/VSEng-vnet/subnets/default";
        }
    }
}
