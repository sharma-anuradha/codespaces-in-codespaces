using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class VmImageFamilyTests
    {
        [Fact]
        public void GetCurrentImageUrl_DoesNotExceedLengthLimit_ForWindowsImages()
        {
            var locations = new List<AzureLocation>
            {
                AzureLocation.EastUs,
                AzureLocation.SouthEastAsia,
                AzureLocation.WestEurope,
                AzureLocation.WestUs2,
            };

            var planeInfo = new Mock<IControlPlaneInfo>();
            planeInfo
                .Setup(x => x.EnvironmentResourceGroupName)
                .Returns("vsclk-online-prod");

            var stampSettings = new ControlPlaneStampSettings
            {
                DnsHostName = "X",
                StampName = "Y"
            };
            stampSettings.DataPlaneLocations.AddRange(locations);

            var subscriptionId = Guid.NewGuid().ToString();

            foreach (var location in locations)
            {
                var stampInfo = new Mock<ControlPlaneStampInfo>(planeInfo.Object, location, "Z", stampSettings);

                var imageFamily = new VmImageFamily(
                    stampInfo.Object,
                    "nexusWindowsImage",
                    VmImageKind.Custom,
                    "NexusWindowsImage",
                    "2019.1111.001",
                    subscriptionId
                    );

                var url = imageFamily.GetCurrentImageUrl(location);

                Assert.True(url.Length < 256, $"Expected length for {url} to be < 256");
            }
        }
    }
}
