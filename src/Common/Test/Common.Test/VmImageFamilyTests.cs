using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class VmImageFamilyTests
    {
        [Fact]
        public async Task GetCurrentImageUrl_DoesNotExceedLengthLimit_ForWindowsImages()
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

            var currentImageInfoProvider = new Mock<ICurrentImageInfoProvider>();
            currentImageInfoProvider
                .Setup(x => x.GetImageVersionAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultVersion, IDiagnosticsLogger logger) => Task.FromResult(defaultVersion));

            foreach (var location in locations)
            {
                var stampInfo = new Mock<ControlPlaneStampInfo>(planeInfo.Object, location, "Z", stampSettings);

                var imageFamily = new VmImageFamily(
                    stampInfo.Object,
                    "nexusWindowsImage",
                    ImageKind.Custom,
                    "NexusWindowsImage",
                    "2019.1111.001",
                    "16.7.1",
                    subscriptionId,
                    currentImageInfoProvider.Object
                    );

                var url = await imageFamily.GetCurrentImageUrlAsync(location, logger: null);

                Assert.True(url.Length < 256, $"Expected length for {url} to be < 256");
            }
        }
    }
}
