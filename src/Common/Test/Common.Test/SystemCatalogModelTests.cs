using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.Azure.Management.Storage.Fluent.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class SystemCatalogModelTests
    {
        [Fact]
        public void Ctor_throws_if_null()
        {
            var skuCatalog = new Mock<ISkuCatalog>().Object;
            var azureSubscriptonCatalog = new Mock<IAzureSubscriptionCatalog>().Object;

            Assert.Throws<ArgumentNullException>(() => new SystemCatalogProvider(null, null));
            Assert.Throws<ArgumentNullException>(() => new SystemCatalogProvider(null, skuCatalog));
            Assert.Throws<ArgumentNullException>(() => new SystemCatalogProvider(azureSubscriptonCatalog, null));
        }

        [Fact]
        public void Ctor_ok()
        {
            var skuCatalog = new Mock<ISkuCatalog>().Object;
            var azureSubscriptonCatalog = new Mock<IAzureSubscriptionCatalog>().Object;
            var provider = new SystemCatalogProvider(azureSubscriptonCatalog, skuCatalog);
            Assert.NotNull(provider);
        }

        [Fact]
        public void Ctor_with_good_options()
        {
            var provider = CreateTestSystemCatalogProvider();
            Assert.NotNull(provider.AzureSubscriptionCatalog);
            Assert.NotNull(provider.AzureSubscriptionCatalog.AzureSubscriptions);
            Assert.NotNull(provider.SkuCatalog);
            Assert.NotNull(provider.SkuCatalog.CloudEnvironmentSkus);
        }

        [Fact]
        public void AzureSubscriptions_OK()
        {
            var provider = CreateTestSystemCatalogProvider();

            // This provider implementation returns a sorSted order
            Assert.Collection(provider.AzureSubscriptionCatalog.AzureSubscriptions,
                s => {
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
        }

        [Fact]
        public void SystemCatalog_OK()
        {
            var provider = CreateTestSystemCatalogProvider();
            Assert.Collection(provider.SkuCatalog.CloudEnvironmentSkus.Values,
                sku =>
                {
                    Assert.Equal(1.0m, sku.StorageCloudEnvironmentUnits);
                    Assert.Equal(10.0m, sku.ComputeCloudEnvironmentUnits);
                    Assert.Equal("test-compute-sku-family-1", sku.ComputeSkuFamily);
                    Assert.Equal("test-compute-sku-name-1", sku.ComputeSkuName);
                    Assert.Equal("test-compute-sku-size-1", sku.ComputeSkuSize);
                    Assert.Equal(ComputeOS.Linux, sku.ComputeOS);
                    Assert.Equal("test-sku-display-name-1", sku.SkuDisplayName);
                    Assert.Equal("test-sku-name-1", sku.SkuName);
                    Assert.Equal(1, sku.StorageSizeInGB);
                    Assert.Equal("test-storage-sku-name-1", sku.StorageSkuName);
                    // Assert the default vm image
                    Assert.Equal("default-vm-image-linux", sku.DefaultVMImage);
                    // Assert the default pool level
                    Assert.Equal(1, sku.PoolLevel);
                    // Assert the default locations
                    Assert.Collection(sku.SkuLocations,
                        loc => Assert.Equal(AzureLocation.EastUs, loc),
                        loc => Assert.Equal(AzureLocation.WestUs2, loc)
                    );
                },
                sku =>
                {
                    Assert.Equal(2.0m, sku.StorageCloudEnvironmentUnits);
                    Assert.Equal(20.0m, sku.ComputeCloudEnvironmentUnits);
                    Assert.Equal("test-compute-sku-family-2", sku.ComputeSkuFamily);
                    Assert.Equal("test-compute-sku-name-2", sku.ComputeSkuName);
                    Assert.Equal("test-compute-sku-size-2", sku.ComputeSkuSize);
                    Assert.Equal(ComputeOS.Linux, sku.ComputeOS);
                    Assert.Equal("test-sku-display-name-2", sku.SkuDisplayName);
                    Assert.Equal("test-sku-name-2", sku.SkuName);
                    Assert.Equal(2, sku.StorageSizeInGB);
                    Assert.Equal("test-storage-sku-name-2", sku.StorageSkuName);
                    // Assert the override vm image
                    Assert.Equal("override-vm-image-linux", sku.DefaultVMImage);
                    // Assert the override pool level
                    Assert.Equal(2, sku.PoolLevel);
                    // Assert the override locations
                    Assert.Collection(sku.SkuLocations,
                        loc => Assert.Equal(AzureLocation.WestEurope, loc));
                });
        }

        [Fact]
        public void SystemCatalog_MissingDefaultVMImage()
        {
            var skuCatalogSettings = CreateSkuCatalogSettings();
            skuCatalogSettings.DefaultSkuConfiguration.VMImages.Clear();
            Assert.Throws<InvalidOperationException>(() => CreateTestSystemCatalogProvider(null, skuCatalogSettings));
            skuCatalogSettings.DefaultSkuConfiguration.VMImages.Add(ComputeOS.Windows, "windows-image"); // still doesn't have linux
            Assert.Throws<InvalidOperationException>(() => CreateTestSystemCatalogProvider(null, skuCatalogSettings));
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

            var clientSecret = await servicePrincipal.GetServicePrincipalClientSecretAsync();
            Assert.Equal(expectedValue, clientSecret);
        }

        [Fact]
        public async Task GetServicePrincipalClientSecret()
        {
            var provider = CreateTestSystemCatalogProvider();

            var clientSecret = await provider.AzureSubscriptionCatalog.AzureSubscriptions.First().ServicePrincipal.GetServicePrincipalClientSecretAsync();
            Assert.Equal(TestSecretValue, clientSecret);
        }

        private const string TestSecretValue = "secret-value";

        private static SystemCatalogProvider CreateTestSystemCatalogProvider(
            DataPlaneSettings dataPlaneSettings = default,
            SkuCatalogSettings skuCatalogSettings = default,
            ServicePrincipalSettings servicePrincipalSettings = default)
        {
            dataPlaneSettings = dataPlaneSettings ?? CreateDataPlaneSettings();
            skuCatalogSettings = skuCatalogSettings ?? CreateSkuCatalogSettings();
            servicePrincipalSettings = servicePrincipalSettings ?? CreateServicePrincipalSettings();
            var secretProvider = new Mock<ISecretProvider>();
            secretProvider
                .Setup(sp => sp.GetSecretAsync(It.IsAny<string>()))
                .ReturnsAsync(() => TestSecretValue);
            var controlPlaneInfo = new Mock<IControlPlaneInfo>();
            controlPlaneInfo
                .Setup(obj => obj.GetAllDataPlaneLocations())
                .Returns(
                    new[] {
                        AzureLocation.EastUs,
                        AzureLocation.SouthEastAsia,
                        AzureLocation.WestEurope,
                        AzureLocation.WestUs2,
                    });

            var azureSubscriptionCatalogOptions = new AzureSubscriptionCatalogOptions
            {
                ApplicationServicePrincipal = servicePrincipalSettings,
                DataPlaneSettings = dataPlaneSettings,
            };

            var azureSubscriptionCatalog = new AzureSubscriptionCatalog(azureSubscriptionCatalogOptions, secretProvider.Object);
            var skuCatalog = new SkuCatalog(skuCatalogSettings, controlPlaneInfo.Object);
            var provider = new SystemCatalogProvider(azureSubscriptionCatalog, skuCatalog);
            return provider;
        }

        private static SkuCatalogSettings CreateSkuCatalogSettings()
        {
            var skuCatalogSettings = new SkuCatalogSettings
            {
                DefaultSkuConfiguration = new SkuConfigurationSettings
                {
                    Locations =
                    {
                        AzureLocation.EastUs,
                        AzureLocation.WestUs2,
                        AzureLocation.UaeNorth, // should get filtered out by IDataPlaneManager.GetAllDataPlaneLocations
                    },
                    PoolSize = 1,
                    VMImages =
                    {
                        { ComputeOS.Linux, "default-vm-image-linux" },
                        { ComputeOS.Windows, "default-vm-image-windows" },
                    }
                },
                CloudEnvironmentSkuSettings =
                {
                    {
                        "test-sku-name-1",
                        new SkuSettings
                        {
                            StorageCloudEnvironmentUnits = 1.0m,
                            ComputeCloudEnvironmentUnits = 10.0m,
                            ComputeSkuFamily = "test-compute-sku-family-1",
                            ComputeSkuName = "test-compute-sku-name-1",
                            ComputeSkuSize = "test-compute-sku-size-1",
                            ComputeOS = ComputeOS.Linux,
                            SkuDisplayName = "test-sku-display-name-1",
                            StorageSizeInGB = 1,
                            StorageSkuName = "test-storage-sku-name-1",
                        }
                    },
                    {
                        "test-sku-name-2",
                        new SkuSettings
                        {
                            StorageCloudEnvironmentUnits = 2.0m,
                            ComputeCloudEnvironmentUnits = 20.0m,
                            ComputeSkuFamily = "test-compute-sku-family-2",
                            ComputeSkuName = "test-compute-sku-name-2",
                            ComputeSkuSize = "test-compute-sku-size-2",
                            ComputeOS = ComputeOS.Linux,
                            SkuDisplayName = "test-sku-display-name-2",
                            StorageSizeInGB = 2,
                            StorageSkuName = "test-storage-sku-name-2",
                            SkuConfiguration = new SkuConfigurationSettings
                            {
                                Locations =
                                {
                                    AzureLocation.WestEurope
                                },
                                PoolSize = 2,
                                VMImages = {
                                    { ComputeOS.Linux, "override-vm-image-linux" }
                                }
                            }
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
                }
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
