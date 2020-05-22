using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class SystemCatalogModelTests
    {
        private static readonly string SubscriptionId = Guid.NewGuid().ToString();

        [Fact]
        public void Ctor_throws_if_null()
        {
            var skuCatalog = new Mock<ISkuCatalog>().Object;
            var azureSubscriptonCatalog = new Mock<IAzureSubscriptionCatalog>().Object;
            var planSkuCatalog = new Mock<IPlanSkuCatalog>().Object;

            Assert.Throws<ArgumentNullException>(() => new SystemCatalogProvider(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new SystemCatalogProvider(azureSubscriptonCatalog, null, null));
            Assert.Throws<ArgumentNullException>(() => new SystemCatalogProvider(azureSubscriptonCatalog, skuCatalog, null));
            Assert.Throws<ArgumentNullException>(() => new SystemCatalogProvider(azureSubscriptonCatalog, null, planSkuCatalog));
            Assert.Throws<ArgumentNullException>(() => new SystemCatalogProvider(null, skuCatalog, null));
            Assert.Throws<ArgumentNullException>(() => new SystemCatalogProvider(null, skuCatalog, planSkuCatalog));
            Assert.Throws<ArgumentNullException>(() => new SystemCatalogProvider(null, null, planSkuCatalog));
        }

        [Fact]
        public void Ctor_ok()
        {
            var skuCatalog = new Mock<ISkuCatalog>().Object;
            var azureSubscriptonCatalog = new Mock<IAzureSubscriptionCatalog>().Object;
            var planSkuCatalog = new Mock<IPlanSkuCatalog>().Object;

            var provider = new SystemCatalogProvider(azureSubscriptonCatalog, skuCatalog, planSkuCatalog);
            Assert.NotNull(provider);
        }

        [Fact]
        public void Ctor_with_good_options()
        {
            var provider = CreateTestSystemCatalogProvider();
            Assert.NotNull(provider.AzureSubscriptionCatalog);
            Assert.NotNull(provider.AzureSubscriptionCatalog.AzureSubscriptions);
            Assert.NotNull(provider.AzureSubscriptionCatalog.InfrastructureSubscription);
            Assert.NotNull(provider.SkuCatalog);
            Assert.NotNull(provider.SkuCatalog.CloudEnvironmentSkus);
        }

        [Fact]
        public void AzureSubscriptions_OK()
        {
            var provider = CreateTestSystemCatalogProvider();

            // This provider implementation returns a sorSted order
            Assert.Collection(provider.AzureSubscriptionCatalog.AzureSubscriptions,
                s =>
                {
                    Assert.Equal("test-subscription-display-name-1", s.DisplayName);
                    Assert.True(s.Enabled);
                    Assert.Equal("test-client-id-1", s.ServicePrincipal.ClientId);
                    Assert.Equal("test-tenant-id-1", s.ServicePrincipal.TenantId);
                    Assert.Equal("11111111-1111-1111-1111-111111111111", s.SubscriptionId);
                    Assert.Collection(s.Locations,
                        loc => { Assert.Equal(AzureLocation.EastUs, loc); },
                        loc => { Assert.Equal(AzureLocation.SouthEastAsia, loc); },
                        loc => { Assert.Equal(AzureLocation.WestUs, loc); },
                        loc => { Assert.Equal(AzureLocation.WestUs2, loc); });
                },
                s =>
                {
                    Assert.Equal("test-subscription-display-name-2", s.DisplayName);
                    Assert.True(s.Enabled);
                    Assert.Equal("test-client-id-2", s.ServicePrincipal.ClientId);
                    Assert.Equal("test-tenant-id-2", s.ServicePrincipal.TenantId);
                    Assert.Equal("22222222-2222-2222-2222-222222222222", s.SubscriptionId);
                    Assert.Collection(s.Locations,
                        loc => { Assert.Equal(AzureLocation.EastUs, loc); },
                        loc => { Assert.Equal(AzureLocation.SouthEastAsia, loc); },
                        loc => { Assert.Equal(AzureLocation.WestUs, loc); },
                        loc => { Assert.Equal(AzureLocation.WestUs2, loc); });
                }
            );

            var infrastructure = provider.AzureSubscriptionCatalog.InfrastructureSubscription;
            Assert.Equal("test-subscription-display-name-3", infrastructure.DisplayName);
            Assert.True(infrastructure.Enabled);
            Assert.Equal("test-client-id-3", infrastructure.ServicePrincipal.ClientId);
            Assert.Equal("test-tenant-id-3", infrastructure.ServicePrincipal.TenantId);
            Assert.Equal("33333333-3333-3333-3333-333333333333", infrastructure.SubscriptionId);
            Assert.Collection(infrastructure.Locations,
                loc => { Assert.Equal(AzureLocation.EastUs, loc); },
                loc => { Assert.Equal(AzureLocation.SouthEastAsia, loc); },
                loc => { Assert.Equal(AzureLocation.WestUs, loc); },
                loc => { Assert.Equal(AzureLocation.WestUs2, loc); });
        }

        [Fact]
        public void SystemCatalog_OK()
        {
            var provider = CreateTestSystemCatalogProvider();
            Assert.Collection(provider.SkuCatalog.CloudEnvironmentSkus.Values.OrderBy(s => s.SkuName),
                sku =>
                {
                    Assert.Equal(StaticEnvironmentSku.Name, sku.SkuName);
                    Assert.Equal(decimal.Zero, sku.StorageVsoUnitsPerHour);
                    Assert.Equal(decimal.Zero, sku.ComputeVsoUnitsPerHour);
                },
                async sku =>
                {
                    Assert.Equal("test-sku-linux-standard", sku.SkuName);
                    Assert.Equal(1.0m, sku.StorageVsoUnitsPerHour);
                    Assert.Equal(10.0m, sku.ComputeVsoUnitsPerHour);
                    Assert.Equal("standard-compute-sku-family", sku.ComputeSkuFamily);
                    Assert.Equal("standard-compute-sku-name", sku.ComputeSkuName);
                    Assert.Equal("standard-compute-sku-size", sku.ComputeSkuSize);
                    Assert.Equal(ComputeOS.Linux, sku.ComputeOS);
                    Assert.Equal("test-sku-linux-standard-name", sku.DisplayName);
                    Assert.Equal(64, sku.StorageSizeInGB);
                    Assert.Equal("standard-storage-sku-name", sku.StorageSkuName);
                    // Assert the vm image and storage image
                    Assert.Equal("test-compute-image-family-linux", sku.ComputeImage.ImageFamilyName);
                    Assert.Equal("test-vm-agent-image-family-linux", sku.VmAgentImage.ImageFamilyName);
                    Assert.Equal(ImageKind.Custom, sku.ComputeImage.ImageKind);
                    Assert.Equal("test-storage-image-family-linux", sku.StorageImage.ImageFamilyName);
                    Assert.Equal("test-storage-image-url-linux", await sku.StorageImage.GetCurrentImageNameAsync(logger: null));
                    // Assert the default pool level
                    Assert.Equal(1, sku.ComputePoolLevel);
                    // Assert the default locations
                    Assert.Collection(sku.SkuLocations,
                        loc => Assert.Equal(AzureLocation.EastUs, loc),
                        loc => Assert.Equal(AzureLocation.WestUs2, loc)
                    );
                },
                async sku =>
                {
                    Assert.Equal("test-sku-windows-premium", sku.SkuName);
                    Assert.Equal(2.0m, sku.StorageVsoUnitsPerHour);
                    Assert.Equal(20.0m, sku.ComputeVsoUnitsPerHour);
                    Assert.Equal("premium-compute-sku-family", sku.ComputeSkuFamily);
                    Assert.Equal("premium-compute-sku-name", sku.ComputeSkuName);
                    Assert.Equal("premium-compute-sku-size", sku.ComputeSkuSize);
                    Assert.Equal(ComputeOS.Windows, sku.ComputeOS);
                    Assert.Equal("test-sku-windows-premium-name", sku.DisplayName);
                    Assert.Equal(64, sku.StorageSizeInGB);
                    Assert.Equal("premium-storage-sku-name", sku.StorageSkuName);
                    // Assert the vm image and storage image
                    Assert.Equal("test-compute-image-family-windows", sku.ComputeImage.ImageFamilyName);
                    Assert.Equal("test-vm-agent-image-family-windows", sku.VmAgentImage.ImageFamilyName);
                    Assert.Equal(ImageKind.Custom, sku.ComputeImage.ImageKind);
                    Assert.Equal("test-storage-image-family-windows", sku.StorageImage.ImageFamilyName);
                    Assert.Equal("test-storage-image-url-windows", await sku.StorageImage.GetCurrentImageNameAsync(logger: null));
                    // Assert the override pool level
                    Assert.Equal(1, sku.ComputePoolLevel);
                    // Assert the override locations
                    Assert.Collection(sku.SkuLocations,
                        loc => Assert.Equal(AzureLocation.WestEurope, loc));
                });
        }

        [Fact]
        public void EnabledInternalHardwareSelectsApplicableSkus()
        {
            var provider = CreateTestSystemCatalogProvider();
            Assert.Collection(provider.SkuCatalog.EnabledInternalHardware().Values.OrderBy(s => s.SkuName),
                sku =>
                {
                    Assert.Equal("test-sku-linux-standard", sku.SkuName);
                },
                sku =>
                {
                    Assert.Equal("test-sku-windows-premium", sku.SkuName);
                });
        }

        [Fact]
        public async Task GetServicePrincipalClientSecret_Resolver()
        {
            const string secretName = "secret-name";
            const string expectedValue = "secret-value";
            var secretProvider = new Mock<ISecretProvider>();
            secretProvider
                .Setup(t => t.GetSecretAsync(It.IsAny<string>()))
                .ReturnsAsync(() => expectedValue);

            var servicePrincipal = new ServicePrincipal(
                "test-client-id",
                secretName,
                "test-tenant-id",
                secretProvider.Object);

            var clientSecret = await servicePrincipal.GetClientSecretAsync();
            Assert.Equal(expectedValue, clientSecret);
        }

        [Fact]
        public async Task GetServicePrincipalClientSecret()
        {
            var provider = CreateTestSystemCatalogProvider();

            var clientSecret = await provider.AzureSubscriptionCatalog.AzureSubscriptions.First().ServicePrincipal.GetClientSecretAsync();
            Assert.Equal(TestSecretValue, clientSecret);
        }

        private const string TestSecretValue = "secret-value";

        private static SystemCatalogProvider CreateTestSystemCatalogProvider(
            DataPlaneSettings dataPlaneSettings = default,
            SkuCatalogSettings skuCatalogSettings = default,
            PlanSkuCatalogSettings planSkuCatalogSettings = default,
            ServicePrincipalSettings servicePrincipalSettings = default)
        {
            dataPlaneSettings = dataPlaneSettings ?? CreateDataPlaneSettings();
            skuCatalogSettings = skuCatalogSettings ?? CreateSkuCatalogSettings();
            planSkuCatalogSettings = planSkuCatalogSettings ?? CreatePlanSkuCatalogSettings();
            servicePrincipalSettings = servicePrincipalSettings ?? CreateServicePrincipalSettings();
            var secretProvider = new Mock<ISecretProvider>();
            secretProvider
                .Setup(sp => sp.GetSecretAsync(It.IsAny<string>()))
                .ReturnsAsync(() => TestSecretValue);
            var controlPlaneInfo = new Mock<IControlPlaneInfo>();
            controlPlaneInfo
                .Setup(obj => obj.Stamp.DataPlaneLocations)
                .Returns(
                    new[] {
                        AzureLocation.EastUs,
                        AzureLocation.SouthEastAsia,
                        AzureLocation.WestEurope,
                        AzureLocation.WestUs2,
                    });
            controlPlaneInfo
                .Setup(obj => obj.EnvironmentResourceGroupName)
                .Returns("test-environment-rg");
            var controlPlaneResourceAccessor = new Mock<IControlPlaneAzureResourceAccessor>();
            controlPlaneResourceAccessor
                .Setup(obj => obj.GetCurrentSubscriptionIdAsync())
                .ReturnsAsync(() => SubscriptionId);

            var azureSubscriptionCatalogOptions = new AzureSubscriptionCatalogOptions
            {
                ApplicationServicePrincipal = servicePrincipalSettings,
                DataPlaneSettings = dataPlaneSettings,
            };

            var azureSubscriptionCatalog = new AzureSubscriptionCatalog(azureSubscriptionCatalogOptions, secretProvider.Object);

            var currentImageInfoProvider = new Mock<ICurrentImageInfoProvider>();
            currentImageInfoProvider
                .Setup(x => x.GetImageNameAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultName, IDiagnosticsLogger logger) => Task.FromResult(defaultName));
            currentImageInfoProvider
                .Setup(x => x.GetImageVersionAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultVersion, IDiagnosticsLogger logger) => Task.FromResult(defaultVersion));

            var skuCatalog = new SkuCatalog(
                skuCatalogSettings,
                controlPlaneInfo.Object,
                controlPlaneResourceAccessor.Object,
                currentImageInfoProvider.Object);

            var planSkuCatalog = new PlanSkuCatalog(planSkuCatalogSettings, controlPlaneInfo.Object);

            var provider = new SystemCatalogProvider(azureSubscriptionCatalog, skuCatalog, planSkuCatalog);
            return provider;
        }

        private static PlanSkuCatalogSettings CreatePlanSkuCatalogSettings()
        {
            return new PlanSkuCatalogSettings
            {
                DefaultSkuName = "test-sku-standard",
                DefaultPlanSkuConfiguration = new Dictionary<string, PlanSkuConfigurationSettings>
                {
                    {
                        "test-sku-standard",
                        new PlanSkuConfigurationSettings
                        {
                            Enabled = true,
                            KeyVaultPoolSize = 1,
                            Locations =
                            {
                                AzureLocation.EastUs,
                                AzureLocation.WestUs2,
                                AzureLocation.UaeNorth, // should get filtered out by controlPlaneInfo.Stamp.DataPlaneLocations
                            },
                        }
                    },
                    {
                        "test-sku-premium",
                        new PlanSkuConfigurationSettings
                        {
                            Enabled = true,
                            KeyVaultPoolSize = 1,
                            Locations =
                            {
                                AzureLocation.EastUs,
                                AzureLocation.WestUs2,
                                AzureLocation.UaeNorth, // should get filtered out by controlPlaneInfo.Stamp.DataPlaneLocations
                            },
                        }
                    }
                },
                PlanSkuSettings = new Dictionary<string, PlanSkuSettings>
                {
                    {
                        "test-sku-standard",
                        new PlanSkuSettings
                        {
                            Enabled = true,
                            KeyVaultSkuName = "Standard",
                            PlanSkuConfiguration = new PlanSkuConfigurationSettings
                            {
                                Locations =
                                {
                                    AzureLocation.WestEurope
                                },
                                KeyVaultPoolSize = 1,
                            }
                        }
                    },
                    {
                        "test-sku-premium",
                        new PlanSkuSettings
                        {
                            Enabled = true,
                            KeyVaultSkuName = "Premium",
                            PlanSkuConfiguration = new PlanSkuConfigurationSettings
                            {
                                Locations =
                                {
                                    AzureLocation.WestEurope
                                },
                                KeyVaultPoolSize = 1,
                            }
                        }
                    }
                },
            };
        }

        private static SkuCatalogSettings CreateSkuCatalogSettings()
        {
            var skuCatalogSettings = new SkuCatalogSettings
            {
                DefaultSkuConfiguration = new Dictionary<ComputeOS, SkuConfigurationSettings>
                {
                    {
                        ComputeOS.Linux,
                        new SkuConfigurationSettings
                        {
                            Locations =
                            {
                                AzureLocation.EastUs,
                                AzureLocation.WestUs2,
                                AzureLocation.UaeNorth, // should get filtered out by IControlPlaneInfo.Stamp.DataPlaneLocations
                            },
                            ComputePoolSize = 1,
                            StoragePoolSize = 1,
                            ComputeImageFamily = "test-compute-image-family-linux",
                            StorageImageFamily = "test-storage-image-family-linux",
                            VmAgentImageFamily = "test-vm-agent-image-family-linux",
                        }
                    },
                    {
                        ComputeOS.Windows,
                        new SkuConfigurationSettings
                        {
                            Locations =
                            {
                                AzureLocation.EastUs,
                                AzureLocation.WestUs2,
                                AzureLocation.UaeNorth, // should get filtered out by IDataPlaneManager.GetAllDataPlaneLocations
                            },
                            ComputePoolSize = 1,
                            StoragePoolSize = 1,
                            ComputeImageFamily = "test-compute-image-family-windows",
                            StorageImageFamily = "test-storage-image-family-windows",
                            VmAgentImageFamily = "test-vm-agent-image-family-windows",
                        }
                    },
                },
                SkuTierSettings = new Dictionary<SkuTier, SkuTierSettings>
                {
                    {
                        SkuTier.Standard,
                        new SkuTierSettings
                        {
                            ComputeSkuCores = 4,
                            ComputeSkuFamily = "standard-compute-sku-family",
                            ComputeSkuName = "standard-compute-sku-name",
                            ComputeSkuSize = "standard-compute-sku-size",
                            StorageSizeInGB = 64,
                            StorageSkuName = "standard-storage-sku-name",
                        }
                    },
                    {
                        SkuTier.Premium,
                        new SkuTierSettings
                        {
                            ComputeSkuCores = 8,
                            ComputeSkuFamily = "premium-compute-sku-family",
                            ComputeSkuName = "premium-compute-sku-name",
                            ComputeSkuSize = "premium-compute-sku-size",
                            StorageSizeInGB = 64,
                            StorageSkuName = "premium-storage-sku-name",
                        }
                    }
                },
                ComputeImageFamilies = new Dictionary<string, VmImageFamilySettings>
                {
                    {
                        "test-compute-image-family-linux",
                        new VmImageFamilySettings
                        {
                            ImageKind = ImageKind.Custom,
                            ImageName = "test-compute-image-url-linux",
                            ImageVersion = "1.0.1",
                        }
                    },
                    {
                        "test-compute-image-family-windows",
                        new VmImageFamilySettings
                        {
                            ImageKind = ImageKind.Custom,
                            ImageName = "test-compute-image-url-windows",
                            ImageVersion = "1.0.1",
                        }
                    },
                },
                VmAgentImageFamilies = new Dictionary<string, ImageFamilySettings>
                {
                    {
                        "test-vm-agent-image-family-linux",
                        new ImageFamilySettings
                        {
                            ImageName = "test-vm-image-url-linux",
                        }
                    },
                    {
                        "test-vm-agent-image-family-windows",
                        new ImageFamilySettings
                        {
                            ImageName = "test-vm-image-url-windows",
                        }
                    },
                },
                StorageImageFamilies = new Dictionary<string, ImageFamilySettings>
                {
                    {
                        "test-storage-image-family-linux",
                        new VmImageFamilySettings
                        {
                            ImageName = "test-storage-image-url-linux",
                        }
                    },
                    {
                        "test-storage-image-family-windows",
                        new VmImageFamilySettings
                        {
                            ImageName = "test-storage-image-url-windows",
                        }
                    },
                },
                CloudEnvironmentSkuSettings =
                {
                    {
                        "test-sku-linux-standard",
                        new SkuSettings
                        {
                            ComputeOS = ComputeOS.Linux,
                            Tier = SkuTier.Standard,
                            DisplayName = "test-sku-linux-standard-name",
                            StorageVsoUnitsPerHour = 1.0m,
                            ComputeVsoUnitsPerHour = 10.0m,
                            Priority = 1
                        }
                    },
                    {
                        "test-sku-windows-premium",
                        new SkuSettings
                        {
                            ComputeOS = ComputeOS.Windows,
                            Tier = SkuTier.Premium,
                            DisplayName = "test-sku-windows-premium-name",
                            StorageVsoUnitsPerHour = 2.0m,
                            ComputeVsoUnitsPerHour = 20.0m,
                            SkuConfiguration = new SkuConfigurationSettings
                            {
                                Locations =
                                {
                                    AzureLocation.WestEurope
                                },
                                ComputePoolSize = 1,
                                StoragePoolSize = 1,
                            },
                            Priority = 2
                        }
                    },
                },
            };
            return skuCatalogSettings;
        }

        private static DataPlaneSettings CreateDataPlaneSettings()
        {
            var locations = new List<AzureLocation>()
            {
                AzureLocation.WestUs2,
                AzureLocation.SouthEastAsia,
                AzureLocation.EastUs,
                AzureLocation.WestUs,
            };

            var azureSubscriptionCatalogSettings = new DataPlaneSettings
            {
                Subscriptions = {
                    {
                        "test-subscription-display-name-1",
                        new AzureSubscriptionSettings
                        {
                            SubscriptionId = "11111111-1111-1111-1111-111111111111",
                            ServicePrincipal = new ServicePrincipalSettings
                            {
                                ClientId = "test-client-id-1",
                                ClientSecretName = "test-client-secret-id-1",
                                TenantId = "test-tenant-id-1"
                            },
                            Locations = locations,
                        }
                    },
                    {  "test-subscription-display-name-2",
                        new AzureSubscriptionSettings
                        {
                            SubscriptionId = "22222222-2222-2222-2222-222222222222",
                            ServicePrincipal = new ServicePrincipalSettings
                            {
                                ClientId = "test-client-id-2",
                                ClientSecretName = "test-client-secret-id-2",
                                TenantId = "test-tenant-id-2"
                            },
                            Locations = locations
                        }
                    }
                },
                InfrastructureSubscription = new AzureSubscriptionSettings
                {
                    SubscriptionName = "test-subscription-display-name-3",
                    SubscriptionId = "33333333-3333-3333-3333-333333333333",
                    ServicePrincipal = new ServicePrincipalSettings
                    {
                        ClientId = "test-client-id-3",
                        ClientSecretName = "test-client-secret-id-3",
                        TenantId = "test-tenant-id-3"
                    },
                    Locations = locations,
                },
            };
            return azureSubscriptionCatalogSettings;
        }

        private static ServicePrincipalSettings CreateServicePrincipalSettings()
        {
            return new ServicePrincipalSettings
            {
                ClientId = "33333333-3333-3333-3333-333333333333",
                ClientSecretName = "secret-name",
                TenantId = "44444444-4444-4444-4444-444444444444",
            };
        }
    }
}
