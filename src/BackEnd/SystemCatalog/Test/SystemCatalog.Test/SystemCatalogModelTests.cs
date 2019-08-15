using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Test
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
        public void AzureSubscriptions_DuplicateSubscription()
        {
            var azureSubscriptionSettings = CreateAzureSubscriptionCatalogSettings();
            azureSubscriptionSettings.AzureSubscriptions[0].SubscriptionId = azureSubscriptionSettings.AzureSubscriptions[1].SubscriptionId;
            Assert.Throws<InvalidOperationException>(() => CreateTestSystemCatalogProvider(azureSubscriptionSettings, null));
        }

        [Fact]
        public void SystemCatalog_OK()
        {
            var provider = CreateTestSystemCatalogProvider();
            Assert.Collection(provider.SkuCatalog.CloudEnvironmentSkus,
                sku =>
                {
                    Assert.Equal(1.0m, sku.StorageCloudEnvironmentUnits);
                    Assert.Equal(10.0m, sku.ComputeCloudEnvironmentUnits);
                    Assert.Equal("test-compute-sku-family-1", sku.ComputeSkuFamily);
                    Assert.Equal("test-compute-sku-name-1", sku.ComputeSkuName);
                    Assert.Equal("test-compute-sku-size-1", sku.ComputeSkuSize);
                    Assert.Equal(ComputeOS.Linux, sku.ComputeOS);
                    Assert.Equal("default-vm-image-linux", sku.DefaultVMImage);
                    Assert.Equal("test-sku-display-name-1", sku.SkuDisplayName);
                    Assert.Equal("test-sku-name-1", sku.SkuName);
                    Assert.Equal(1, sku.StorageSizeInGB);
                    Assert.Equal("test-storage-sku-name-1", sku.StorageSkuName);
                    // Check the default locations
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
                    Assert.Equal("default-vm-image-linux", sku.DefaultVMImage);
                    Assert.Equal("test-sku-display-name-2", sku.SkuDisplayName);
                    Assert.Equal("test-sku-name-2", sku.SkuName);
                    Assert.Equal(2, sku.StorageSizeInGB);
                    Assert.Equal("test-storage-sku-name-2", sku.StorageSkuName);
                    // Assert the override locations
                    Assert.Collection(sku.SkuLocations,
                        loc => Assert.Equal(AzureLocation.WestEurope, loc));
                });
        }

        [Fact]
        public void SystemCatalog_MissingDefaultVMImage()
        {
            var skuCatalogSettings = CreateSkuCatalogSettings();
            skuCatalogSettings.DefaultVMImages.Clear();
            Assert.Throws<InvalidOperationException>(() => CreateTestSystemCatalogProvider(null, skuCatalogSettings));
            skuCatalogSettings.DefaultVMImages.Add(ComputeOS.Windows, "windows-image"); // still doesn't have linux
            Assert.Throws<InvalidOperationException>(() => CreateTestSystemCatalogProvider(null, skuCatalogSettings));
        }

        [Fact]
        public void SystemCatalog_DuplicateSKU()
        {
            var skuCatalogSettings = new SkuCatalogSettings
            {
                CloudEnvironmentSkuSettings =
                    {
                        new CloudEnvironmentSkuSettings
                        {
                            StorageCloudEnvironmentUnits = 1.0m,
                            ComputeCloudEnvironmentUnits = 10.0m,
                            ComputeSkuFamily = "test-compute-sku-family-1",
                            ComputeSkuName = "test-compute-sku-name-1",
                            ComputeSkuSize = "test-compute-sku-size-1",
                            ComputeOS = ComputeOS.Linux,
                            SkuDisplayName = "test-sku-display-name-1",
                            SkuName = "test-sku-name-1",
                            StorageSizeInGB = 1,
                            StorageSkuName = "test-storage-sku-name-1",
                        },
                        new CloudEnvironmentSkuSettings
                        {
                            StorageCloudEnvironmentUnits = 1.0m,
                            ComputeCloudEnvironmentUnits = 10.0m,
                            ComputeSkuFamily = "test-compute-sku-family-1",
                            ComputeSkuName = "test-compute-sku-name-1",
                            ComputeSkuSize = "test-compute-sku-size-1",
                            ComputeOS = ComputeOS.Linux,
                            SkuDisplayName = "test-sku-display-name-1",
                            SkuName = "test-sku-name-1",
                            StorageSizeInGB = 1,
                            StorageSkuName = "test-storage-sku-name-1",
                        },
                    },
                    DefaultPoolLevel = 1,
                    DefaultVMImages =
                    {
                        {  ComputeOS.Linux, "linux-image" },
                    }
            };
            Assert.Throws<InvalidOperationException>(() => CreateTestSystemCatalogProvider(null, skuCatalogSettings));
        }

        [Fact]
        public async Task GetServicePrincipalClientSecret_Resolver()
        {
            var servicePrincipal = new ServicePrincipal(
                "test-client-id",
                "test-client-secret-id",
                "test-tenant-id",
                id => Task.FromResult(id + "-secret"));

            var clientSecret = await servicePrincipal.GetServicePrincipalClientSecret();
            Assert.Equal("test-client-secret-id-secret", clientSecret);
        }

        [Fact]
        public async Task GetServicePrincipalClientSecret_From_Envrionemnt()
        {
            var provider = CreateTestSystemCatalogProvider();

            // TODO: Need a better way to inject and mock the secret resolver!
            // This is a race condition that would affect other tests, so keep all client-secret tests in one function

            Environment.SetEnvironmentVariable("test-client-secret-id-1", null);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await provider.AzureSubscriptionCatalog.AzureSubscriptions.First().ServicePrincipal.GetServicePrincipalClientSecret();
            });

            Environment.SetEnvironmentVariable("test-client-secret-id-1", "test-secret-1");
            var clientSecret = await provider.AzureSubscriptionCatalog.AzureSubscriptions.First().ServicePrincipal.GetServicePrincipalClientSecret();
            Assert.Equal("test-secret-1", clientSecret);
        }

        private static SystemCatalogProvider CreateTestSystemCatalogProvider(
            AzureSubscriptionCatalogSettings azureSubscriptionCatalogSettings = default,
            SkuCatalogSettings skuCatalogSettings = default)
        {
            azureSubscriptionCatalogSettings = azureSubscriptionCatalogSettings ?? CreateAzureSubscriptionCatalogSettings();
            skuCatalogSettings = skuCatalogSettings ?? CreateSkuCatalogSettings();

            var azureSubscriptionCatalog = new AzureSubscriptionCatalog(azureSubscriptionCatalogSettings);
            var skuCatalog = new SkuCatalog(skuCatalogSettings);
            var provider = new SystemCatalogProvider(azureSubscriptionCatalog, skuCatalog);
            return provider;
        }

        private static SkuCatalogSettings CreateSkuCatalogSettings()
        {
            var skuCatalogSettings = new SkuCatalogSettings
            {
                DefaultLocations =
                {
                    AzureLocation.EastUs,
                    AzureLocation.WestUs2,
                },
                CloudEnvironmentSkuSettings =
                {
                    new CloudEnvironmentSkuSettings
                    {
                        StorageCloudEnvironmentUnits = 1.0m,
                        ComputeCloudEnvironmentUnits = 10.0m,
                        ComputeSkuFamily = "test-compute-sku-family-1",
                        ComputeSkuName = "test-compute-sku-name-1",
                        ComputeSkuSize = "test-compute-sku-size-1",
                        ComputeOS = ComputeOS.Linux,
                        SkuDisplayName = "test-sku-display-name-1",
                        SkuName = "test-sku-name-1",
                        StorageSizeInGB = 1,
                        StorageSkuName = "test-storage-sku-name-1",
                    },
                    new CloudEnvironmentSkuSettings
                    {
                        StorageCloudEnvironmentUnits = 2.0m,
                        ComputeCloudEnvironmentUnits = 20.0m,
                        ComputeSkuFamily = "test-compute-sku-family-2",
                        ComputeSkuName = "test-compute-sku-name-2",
                        ComputeSkuSize = "test-compute-sku-size-2",
                        ComputeOS = ComputeOS.Linux,
                        SkuDisplayName = "test-sku-display-name-2",
                        SkuName = "test-sku-name-2",
                        StorageSizeInGB = 2,
                        StorageSkuName = "test-storage-sku-name-2",
                        OverrideLocations =
                        {
                            AzureLocation.WestEurope
                        }
                    }
                },
                DefaultPoolLevel = 1,
                DefaultVMImages =
                {
                    { ComputeOS.Linux, "default-vm-image-linux" },
                    { ComputeOS.Windows, "default-vm-image-windows" },
                }
            };
            return skuCatalogSettings;
        }

        private static AzureSubscriptionCatalogSettings CreateAzureSubscriptionCatalogSettings()
        {
            var azureSubscriptionCatalogSettings = new AzureSubscriptionCatalogSettings
            {
                DefaultLocations =
                    {
                        AzureLocation.WestUs2,
                        AzureLocation.SouthEastAsia,
                        AzureLocation.EastUs,
                        AzureLocation.WestUs,
                    },
                AzureSubscriptions =
                    {
                        new AzureSubscriptionSettings
                        {
                            SubscriptionId = "11111111-1111-1111-1111-111111111111",
                            DisplayName = "test-subscription-display-name-1",
                            ServicePrincipal = new ServicePrincipalSettings
                            {
                                ClientId = "test-client-id-1",
                                ClientSecretKeyVaultSecretIdentifier = "test-client-secret-id-1",
                                TenantId = "test-tenant-id-1"
                            }
                        },
                        new AzureSubscriptionSettings
                        {
                            SubscriptionId = "22222222-2222-2222-2222-222222222222",
                            DisplayName = "test-subscription-display-name-2",
                            ServicePrincipal = new ServicePrincipalSettings
                            {
                                ClientId = "test-client-id-2",
                                ClientSecretKeyVaultSecretIdentifier = "test-client-secret-id-2",
                                TenantId = "test-tenant-id-2"
                            }
                        }
                    }
            };
            return azureSubscriptionCatalogSettings;
        }
    }
}
