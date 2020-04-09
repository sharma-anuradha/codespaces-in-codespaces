using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Contracts;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class ProfileUtilTests
    {
        private readonly string internalWindowsSkuName = "internalWindows";
        private readonly string internal64ServerSkuName = "internal64Server";
        private readonly string internal32ServerSkuName = "internal32Server";

        private readonly Profile msProfile = new Profile
        {
            Email = "coder@microsoft.com"
        };

        private readonly Profile ms2Profile = new Profile
        {
            Email = "coder@int.microsoft.com"
        };

        private readonly Profile outsideProfile = new Profile
        {
            Email = "person@gmail.com"
        };

        private readonly Profile fakeProfile = new Profile
        {
            Email = "person@fakemicrosoft.com"
        };

        private readonly Profile featureFlagEnabledProfile = new Profile
        {
            Email = "external@gmail.com",
            Programs = new Dictionary<string, object>()
            {
                {
                    ProfileExtensions.VisualStudioOnlineInternalWindowsSkuUserProgram,
                    true
                },
            },
        };

        private readonly Profile nullEmailProfile = new Profile
        {
            Email = null
        };

        private Mock<ICurrentImageInfoProvider> currentImageInfoProvider = new Mock<ICurrentImageInfoProvider>();

        private IControlPlaneInfo MockControlPlaneInfo()
        {
            var moq = new Mock<IControlPlaneInfo>();
            var moq2 = new Mock<IControlPlaneStampInfo>();
            moq
                .Setup(obj => obj.GetOwningControlPlaneStamp(It.IsAny<AzureLocation>()))
                .Returns((AzureLocation location) =>
                {
                    if (location == AzureLocation.WestUs2)
                    {
                        moq2
                            .Setup(obj2 => obj2.Location)
                            .Returns(location);
                        return moq2.Object;
                    }

                    throw new NotSupportedException();
                });
            moq
                .Setup(obj => obj.Stamp)
                .Returns(moq2.Object);

            return moq.Object;
        }

        [Fact]
        public void InternalWindowsSkuVisibility()
        {
            var windowsSku = new CloudEnvironmentSku(internalWindowsSkuName,
                SkuTier.Premium,
                internalWindowsSkuName,
                true,
                new[] { AzureLocation.WestUs2 },
                "computeSkuFamily",
                "computeSkuName",
                "computeSkuSize",
                4,
                Common.Contracts.ComputeOS.Windows,
                new BuildArtifactImageFamily(
                    ImageFamilyType.VmAgent,
                    "agentImageFamily",
                    "agentImageName",
                    "computeImageVersion",
                    currentImageInfoProvider.Object),
                new VmImageFamily(
                    MockControlPlaneInfo().Stamp,
                    "vmImageFamilyName",
                    ImageKind.Canonical,
                    "vmImageName",
                    "vmImageVersion",
                    "vmImageSubscriptionId",
                    currentImageInfoProvider.Object),
                "storageSkuName",
                new BuildArtifactImageFamily(
                    ImageFamilyType.Storage,
                    "storageImageFamily",
                    "storageImageName",
                    null,
                    currentImageInfoProvider.Object),
                64,
                0.0m,
                0.0m,
                5,
                5,
                new ReadOnlyCollection<string>(new string[0]),
                new ReadOnlyCollection<string>(new string[0]),
                1);

            Assert.True(ProfileUtils.IsSkuVisibleToProfile(msProfile, windowsSku));
            Assert.True(ProfileUtils.IsSkuVisibleToProfile(ms2Profile, windowsSku));
            Assert.True(ProfileUtils.IsSkuVisibleToProfile(featureFlagEnabledProfile, windowsSku));
            Assert.False(ProfileUtils.IsSkuVisibleToProfile(outsideProfile, windowsSku));
            Assert.False(ProfileUtils.IsSkuVisibleToProfile(fakeProfile, windowsSku));
            Assert.False(ProfileUtils.IsSkuVisibleToProfile(nullEmailProfile, windowsSku));
        }

        [Fact]
        public void Internal64ServerSkuVisibility()
        {
            var windows64Sku = new CloudEnvironmentSku(internal64ServerSkuName,
                SkuTier.Premium,
                internal64ServerSkuName,
                true,
                new[] { AzureLocation.WestUs2 },
                "computeSkuFamily",
                "computeSkuName",
                "computeSkuSize",
                4,
                Common.Contracts.ComputeOS.Windows,
                new BuildArtifactImageFamily(
                    ImageFamilyType.VmAgent,
                    "agentImageFamily",
                    "agentImageName",
                    "computeImageVersion",
                    currentImageInfoProvider.Object),
                new VmImageFamily(
                    MockControlPlaneInfo().Stamp,
                    "vmImageFamilyName",
                    ImageKind.Canonical,
                    "vmImageName",
                    "vmImageVersion",
                    "vmImageSubscriptionId",
                    currentImageInfoProvider.Object),
                "storageSkuName",
                new BuildArtifactImageFamily(
                    ImageFamilyType.Storage,
                    "storageImageFamily",
                    "storageImageName",
                    null,
                    currentImageInfoProvider.Object),
                64,
                0.0m,
                0.0m,
                5,
                5,
                new ReadOnlyCollection<string>(new string[0]),
                new ReadOnlyCollection<string>(new string[0]),
                1);

            Assert.True(ProfileUtils.IsSkuVisibleToProfile(msProfile, windows64Sku));
            Assert.True(ProfileUtils.IsSkuVisibleToProfile(ms2Profile, windows64Sku));
            Assert.True(ProfileUtils.IsSkuVisibleToProfile(featureFlagEnabledProfile, windows64Sku));
            Assert.False(ProfileUtils.IsSkuVisibleToProfile(outsideProfile, windows64Sku));
            Assert.False(ProfileUtils.IsSkuVisibleToProfile(fakeProfile, windows64Sku));
            Assert.False(ProfileUtils.IsSkuVisibleToProfile(nullEmailProfile, windows64Sku));
        }

        [Fact]
        public void Internal32ServerSkuVisibility()
        {
            var windows32Sku = new CloudEnvironmentSku(internal32ServerSkuName,
                SkuTier.Premium,
                internal32ServerSkuName,
                true,
                new[] { AzureLocation.WestUs2 },
                "computeSkuFamily",
                "computeSkuName",
                "computeSkuSize",
                4,
                Common.Contracts.ComputeOS.Windows,
                new BuildArtifactImageFamily(
                    ImageFamilyType.VmAgent,
                    "agentImageFamily",
                    "agentImageName",
                    "computeImageVersion",
                    currentImageInfoProvider.Object),
                new VmImageFamily(
                    MockControlPlaneInfo().Stamp,
                    "vmImageFamilyName",
                    ImageKind.Canonical,
                    "vmImageName",
                    "vmImageVersion",
                    "vmImageSubscriptionId",
                    currentImageInfoProvider.Object),
                    "storageSkuName",
                new BuildArtifactImageFamily(
                    ImageFamilyType.Storage,
                    "storageImageFamily",
                    "storageImageName",
                    null,
                    currentImageInfoProvider.Object),
                64,
                0.0m,
                0.0m,
                5,
                5,
                new ReadOnlyCollection<string>(new string[0]),
                new ReadOnlyCollection<string>(new string[0]),
                1);

            Assert.True(ProfileUtils.IsSkuVisibleToProfile(msProfile, windows32Sku));
            Assert.True(ProfileUtils.IsSkuVisibleToProfile(ms2Profile, windows32Sku));
            Assert.True(ProfileUtils.IsSkuVisibleToProfile(featureFlagEnabledProfile, windows32Sku));
            Assert.False(ProfileUtils.IsSkuVisibleToProfile(outsideProfile, windows32Sku));
            Assert.False(ProfileUtils.IsSkuVisibleToProfile(fakeProfile, windows32Sku));
            Assert.False(ProfileUtils.IsSkuVisibleToProfile(nullEmailProfile, windows32Sku));
        }
    }
}
