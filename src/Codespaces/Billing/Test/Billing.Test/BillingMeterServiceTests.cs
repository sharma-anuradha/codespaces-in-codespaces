using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using BillingMeter = Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts.BillingMeter;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Test
{
    public class BillingMeterServiceTests
    {
        private readonly decimal standardLinuxComputeUnitPerHr = 125;
        private readonly decimal premiumLinuxComputeUnitPerHr = 242;
        private readonly decimal standardLinuxStorageUnitPerHr = 2;
        private readonly decimal premiumLinuxStorageUnitPerHr = 3;
        private readonly int standadLinuxStorageSizeInGB = 32;
        private readonly int premiumLinuxStorageSizeInGB = 64;
        public static readonly string standardLinuxSkuName = "standardLinuxSku";
        public static readonly string premiumLinuxSkuName = "premiumLinuxSku";

        private readonly IBillingMeterService billingMeterService;
        private readonly IDiagnosticsLogger logger;

        public static readonly VsoPlanInfo testPlan = new VsoPlanInfo
        {
            Subscription = Guid.NewGuid().ToString(),
            ResourceGroup = "testRG",
            Name = "testPlan",
            Location = AzureLocation.WestUs2,
        };

        public BillingMeterServiceTests()
        {
            var loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();

            var skuCatalog = GetMockSkuCatalog();
            var billingMeterCatalog = GetMockBillMeterCatalog();
            billingMeterService = new BillingMeterService(skuCatalog.Object, billingMeterCatalog);
        }

        [Fact]
        public void GetLegacyUsageBasedOnResources()
        {
            var detail = new ResourceUsageDetail()
            {
                Compute = new List<ComputeUsageDetail>()
                {
                    new ComputeUsageDetail() { Usage = 1800, Sku = standardLinuxSkuName }
                },
                Storage = new List<StorageUsageDetail>()
                {
                    new StorageUsageDetail() { Usage = 1800, Sku = standardLinuxSkuName, Size = 32 }
                }
            };

            var result = billingMeterService.GetUsageBasedOnResources(detail, testPlan, DateTime.UtcNow, logger);

            Assert.Equal(1, result.Count);

            var meter = result.Single();

            Assert.Equal("5f3afa79-01ad-4d7e-b691-73feca4ea350", meter.Key);
            Assert.Equal(63.5, meter.Value);            
        }

        [Fact]
        public void GetLegacyUsageBasedOnResources_StorageOnly()
        {
            var detail = new ResourceUsageDetail()
            {
                Storage = new List<StorageUsageDetail>()
                {
                    new StorageUsageDetail() { Usage = 1800, Sku = standardLinuxSkuName, Size = 32 }
                }
            };

            var result = billingMeterService.GetUsageBasedOnResources(detail, testPlan, DateTime.UtcNow, logger);

            Assert.Equal(1, result.Count);

            var meter = result.Single();

            Assert.Equal("5f3afa79-01ad-4d7e-b691-73feca4ea350", meter.Key);
            Assert.Equal(1, meter.Value);
        }

        [Fact]
        public void GetUsageBasedOnResourcesMeters_GitHubOnly()
        {
            var detail = new ResourceUsageDetail()
            {
                Compute = new List<ComputeUsageDetail>()
                {
                    new ComputeUsageDetail() { Usage = 1800, Sku = standardLinuxSkuName }
                },
                Storage = new List<StorageUsageDetail>()
                {
                    new StorageUsageDetail() { Usage = 1800, Sku = standardLinuxSkuName, Size = 32 }
                }
            };

            var skuCatalog = GetMockSkuCatalog();
            var billingMeterCatalog = GetMockBillMeterCatalog(false);
            var billingMtrService = new BillingMeterService(skuCatalog.Object, billingMeterCatalog);
            
            var result = billingMtrService.GetUsageBasedOnResources(detail, testPlan, DateTime.UtcNow, logger, Plans.Contracts.Partner.GitHub);

            // 2 meters should exist
            Assert.Equal(2, result.Count);
            Assert.Contains("compute_Westus2_standardLinuxSku_meterId", result);
            Assert.Contains("storage_Westus2_standardLinuxSku_meterId", result);
            
            //TODO: Assert total bill
        }

        protected Mock<ISkuCatalog> GetMockSkuCatalog()
        {
            var mockStandardLinux = new Mock<ICloudEnvironmentSku>();
            mockStandardLinux.Setup(sku => sku.StorageSizeInGB).Returns(standadLinuxStorageSizeInGB);
            mockStandardLinux.Setup(sku => sku.ComputeVsoUnitsPerHour).Returns(standardLinuxComputeUnitPerHr);
            mockStandardLinux.Setup(sku => sku.StorageVsoUnitsPerHour).Returns(standardLinuxStorageUnitPerHr);

            var mockPremiumLinux = new Mock<ICloudEnvironmentSku>();
            mockPremiumLinux.Setup(sku => sku.StorageSizeInGB).Returns(premiumLinuxStorageSizeInGB);
            mockPremiumLinux.Setup(sku => sku.ComputeVsoUnitsPerHour).Returns(premiumLinuxComputeUnitPerHr);
            mockPremiumLinux.Setup(sku => sku.StorageVsoUnitsPerHour).Returns(premiumLinuxStorageUnitPerHr);

            var skus = new Dictionary<string, ICloudEnvironmentSku>
            {
                [standardLinuxSkuName] = mockStandardLinux.Object,
                [premiumLinuxSkuName] = mockPremiumLinux.Object,
            };
            var mockSkuCatelog = new Mock<ISkuCatalog>();
            mockSkuCatelog.Setup(cat => cat.CloudEnvironmentSkus).Returns(skus);
            return mockSkuCatelog;
        }

        protected IBillingMeterCatalog GetMockBillMeterCatalog(bool useLegeacyMeters = true)
        {
            var billingMeterSettings = new BillingMeterSettings()
            {
                MetersByLocation = new Dictionary<AzureLocation, string>
                {
                    { AzureLocation.WestUs2, "5f3afa79-01ad-4d7e-b691-73feca4ea350" },
                    { AzureLocation.EastUs, "91f658f0-9ce6-4f5d-8795-aa224cb83ccc" },
                    { AzureLocation.WestEurope, "edd2e9c5-56ce-469f-aedb-ba82f4e745cd" },
                    { AzureLocation.SouthEastAsia, "12e4ab51-ee20-4bbc-95a6-9dddea31e634" },
                },
                MetersByResource = new ResourceBillingMeters
                {
                    Compute = new List<BillingMeter>
                    {
                        new BillingMeter
                        {
                            EnabledOnDate = useLegeacyMeters? DateTime.Now.AddDays(1) : DateTime.Now.AddDays(-1),
                            MeterId = "compute_Westus2_standardLinuxSku_meterId",
                            Region = AzureLocation.WestUs2,
                            SkuName = standardLinuxSkuName
                        }
                    },
                    Storage = new List<BillingMeter>
                    { 
                        new BillingMeter
                        {
                            EnabledOnDate = useLegeacyMeters? DateTime.Now.AddDays(1) : DateTime.Now.AddDays(-1),
                            MeterId = "storage_Westus2_standardLinuxSku_meterId",
                            Region = AzureLocation.WestUs2,
                            SkuName = standardLinuxSkuName
                        }
                    }
                }
            };

            var billingOptions = new Mock<IOptions<BillingMeterSettings>>();
            billingOptions.Setup(t => t.Value).Returns(billingMeterSettings);
            var billingMeterCatelog = new BillingMeterCatalog(billingOptions.Object);
            return billingMeterCatelog;

        }
    }
}
