using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Test
{
    public class ResourceTagTests
    {
        [Theory]
        [InlineData(ComputeOS.Linux)]
        [InlineData(ComputeOS.Windows)]
        public void GetComputeOS_Succeeds(ComputeOS computeOS)
        {
            var tags = new Dictionary<string, string>()
            {
                [ResourceTagName.ComputeOS] = computeOS.ToString()
            };

            Assert.Equal(tags.GetComputeOS(), computeOS);
        }

        [Fact]
        public void GetComputeOS_Throws_On_Missing_Tag()
        {
            var tags = new Dictionary<string, string>()
            {
                [ResourceTagName.ResourceName] = "SomeName",
            };

            Assert.Throws<NotSupportedException>(() => tags.GetComputeOS());
        }

        [Fact]
        public void GetComputeOS_Throws_On_Unsupported_OS()
        {
            var tags = new Dictionary<string, string>()
            {
                [ResourceTagName.ComputeOS] = "osx",
            };

            Assert.Throws<NotSupportedException>(() => tags.GetComputeOS());
        }
    }
}
