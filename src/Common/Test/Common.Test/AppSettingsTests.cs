using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class AppSettingsTests
    {
        public static TheoryData<string, string> EnvironmentNames = new TheoryData<string, string>
        {
            // primary appsettings, override appsettings
            { "dev", null },
            { "dev", "dev-ci" },
            { "dev", "dev-stg" },
            { "dev", "local" },
            { "ppe-rel", null },
            { "ppe-rel", "ppe-load" },
            { "prod-rel", null },
            { "prod-rel", "prod-can" },
            { "prod-can", null },
        };

        [Theory]
        [MemberData(nameof(EnvironmentNames))]
        public void AppSettingsSerialization(string environmentName, string overrideName)
        {
            var appSettings = LoadAppSettings(environmentName, overrideName);
            Assert.NotNull(appSettings);
            var json = JsonConvert.SerializeObject(appSettings);
            Assert.NotNull(json);
        }

        [Theory]
        [MemberData(nameof(EnvironmentNames))]
        public void AppSettings_SkuCatalog(string environmentName, string overrideName)
        {
            var appSettings = LoadAppSettings(environmentName, overrideName);
            var controlPlaneInfo = new Mock<IControlPlaneInfo>();
            controlPlaneInfo.Setup(obj => obj.EnvironmentResourceGroupName).Returns("test-environment-rg");
            controlPlaneInfo.Setup(obj => obj.Stamp.DataPlaneLocations).Returns(new List<AzureLocation>());
            var subscriptionId = Guid.NewGuid().ToString();
            var controlPlaneAzureResourceAccessor = new Mock<IControlPlaneAzureResourceAccessor>();
            controlPlaneAzureResourceAccessor.Setup(obj => obj.GetCurrentSubscriptionIdAsync()).Returns(Task.FromResult(subscriptionId));

            var currentImageInfoProvider = new Mock<ICurrentImageInfoProvider>();
            currentImageInfoProvider
                .Setup(x => x.GetImageNameAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultName, IDiagnosticsLogger logger) => Task.FromResult(defaultName));
            currentImageInfoProvider
                .Setup(x => x.GetImageVersionAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultVersion, IDiagnosticsLogger logger) => Task.FromResult(defaultVersion));

            var skuCatalog = new SkuCatalog(
                appSettings.SkuCatalogSettings,
                controlPlaneInfo.Object,
                controlPlaneAzureResourceAccessor.Object,
                currentImageInfoProvider.Object);

            Assert.NotNull(skuCatalog);
            foreach (var item in skuCatalog.CloudEnvironmentSkus)
            {
                var name = item.Key;
                var sku = item.Value;
                Assert.Equal(name, sku.SkuName);
            }
            foreach (var item in skuCatalog.EnabledInternalHardware())
            {
                var sku = item.Value;
                {
                    // for launch we're disabling windows billing
                    if (!sku.SkuName.Equals("standardWindows") &&
                        !sku.SkuName.Equals("premiumWindows") &&
                        !sku.SkuName.Equals("internalWindows") &&
                        !sku.SkuName.Equals("premiumWindowsStaging") &&
                        !sku.SkuName.Equals("premiumWindowsInternalStaging") &&
                        !sku.SkuName.Equals("premiumWindowsServerInternalStaging") &&
                        !sku.SkuName.Equals("internal64Server") &&
                        !sku.SkuName.Equals("internal32Server") &&
                        !sku.SkuName.Equals("internalDailyWindows"))
                    {
                        Assert.True(sku.ComputeVsoUnitsPerHour > 0.0m);
                        Assert.True(sku.StorageVsoUnitsPerHour > 0.0m);
                    }

                    // for prod-can appsettings all sku pool and storage
                    // is defaulted to 0 except Standdard Linux and Windows
                    if (environmentName == "prod-can")
                    {
                        if (sku.SkuName.Equals("premiumLinux") ||
                            sku.SkuName.Equals("basicLinux") ||
                            sku.SkuName.Equals("premiumWindows") ||
                            sku.SkuName.Equals("premiumWindowsStaging") ||
                            sku.SkuName.Equals("premiumWindowsInternalStaging") ||
                            sku.SkuName.Equals("premiumWindowsServerInternalStaging") ||
                            sku.SkuName.Equals("premiumWindows") ||
                            sku.SkuName.Equals("premiumWindowsStaging") ||
                            sku.SkuName.Equals("premiumWindowsInternalStaging"))
                        {
                            Assert.True(sku.StoragePoolLevel == 0);
                            Assert.True(sku.ComputePoolLevel == 0);
                        }
                        else
                        {
                            Assert.True(sku.StoragePoolLevel > 0);
                            Assert.True(sku.ComputePoolLevel > 0);
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(EnvironmentNames))]
        public void AppSettings_SkuCatalogDisabledSkus(string environmentName, string overrideName)
        {
            var appSettings = LoadAppSettings(environmentName, overrideName);
            var controlPlaneInfo = new Mock<IControlPlaneInfo>();
            controlPlaneInfo.Setup(obj => obj.EnvironmentResourceGroupName).Returns("test-environment-rg");
            controlPlaneInfo.Setup(obj => obj.Stamp.DataPlaneLocations).Returns(new List<AzureLocation>());
            var subscriptionId = Guid.NewGuid().ToString();
            var controlPlaneAzureResourceAccessor = new Mock<IControlPlaneAzureResourceAccessor>();
            controlPlaneAzureResourceAccessor.Setup(obj => obj.GetCurrentSubscriptionIdAsync()).Returns(Task.FromResult(subscriptionId));

            var currentImageInfoProvider = new Mock<ICurrentImageInfoProvider>();
            currentImageInfoProvider
                .Setup(x => x.GetImageNameAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultName, IDiagnosticsLogger logger) => Task.FromResult(defaultName));
            currentImageInfoProvider
                .Setup(x => x.GetImageVersionAsync(It.IsAny<ImageFamilyType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns((ImageFamilyType familyType, string family, string defaultVersion, IDiagnosticsLogger logger) => Task.FromResult(defaultVersion));

            var skuCatalog = new SkuCatalog(
                appSettings.SkuCatalogSettings,
                controlPlaneInfo.Object,
                controlPlaneAzureResourceAccessor.Object,
                currentImageInfoProvider.Object);

            Assert.NotNull(skuCatalog);
            var internal32Server = skuCatalog.CloudEnvironmentSkus.FirstOrDefault(s => s.Key.Equals("internal32Server"));
            var internal64Server = skuCatalog.CloudEnvironmentSkus.FirstOrDefault(s => s.Key.Equals("internal64Server"));
            var internalDailyWindows = skuCatalog.CloudEnvironmentSkus.FirstOrDefault(s => s.Key.Equals("internalDailyWindows"));

            Assert.False(internal32Server.Value.Enabled);
            Assert.False(internal64Server.Value.Enabled);
            Assert.False(internalDailyWindows.Value.Enabled);
        }

        [Theory]
        [MemberData(nameof(EnvironmentNames))]
        public void AppSettings_AzureSubscriptionCatalog(string environmentName, string overrideName)
        {
            var appSettings = LoadAppSettings(environmentName, overrideName);
            var options = new AzureSubscriptionCatalogOptions
            {
                DataPlaneSettings = appSettings.DataPlaneSettings,
                ApplicationServicePrincipal = appSettings.ApplicationServicePrincipal,
            };

            var secretProvider = new Mock<ISecretProvider>().Object;
            var subscriptionCatalog = new AzureSubscriptionCatalog(options, secretProvider);
            Assert.NotNull(subscriptionCatalog);
            Assert.NotEmpty(subscriptionCatalog.AzureSubscriptions);
            Assert.NotNull(subscriptionCatalog.InfrastructureSubscription);

            foreach (var subscription in subscriptionCatalog.AzureSubscriptions)
            {
                Assert.NotNull(subscription.ComputeQuotas);
                Assert.NotNull(subscription.NetworkQuotas);
                Assert.NotNull(subscription.StorageQuotas);

                // service specific subscriptions should only have their respective quota type set
                // and should only have a single location specified.
                switch (subscription.ServiceType)
                    {
                        case ServiceType.Compute:
                            Assert.NotEmpty(subscription.ComputeQuotas);
                            Assert.Empty(subscription.NetworkQuotas);
                            Assert.Empty(subscription.StorageQuotas);
                            Assert.Single(subscription.Locations);
                            break;
                        case ServiceType.Network:
                            Assert.NotEmpty(subscription.NetworkQuotas);
                            Assert.Empty(subscription.ComputeQuotas);
                            Assert.Empty(subscription.StorageQuotas);
                            Assert.Single(subscription.Locations);
                            break;
                        case ServiceType.Storage:
                            Assert.NotEmpty(subscription.StorageQuotas);
                            Assert.Empty(subscription.ComputeQuotas);
                            Assert.Empty(subscription.NetworkQuotas);
                            Assert.Single(subscription.Locations);
                            break;
                        case ServiceType.KeyVault:
                            Assert.Empty(subscription.ComputeQuotas);
                            Assert.Empty(subscription.StorageQuotas);
                            Assert.Empty(subscription.NetworkQuotas);
                            Assert.Single(subscription.Locations);
                            break;
                        default:
                            Assert.True(subscription.ComputeQuotas.ContainsKey("standardFSv2Family"));
                            Assert.True(subscription.NetworkQuotas.ContainsKey("VirtualNetworks"));
                            Assert.True(subscription.StorageQuotas.ContainsKey("StorageAccounts"));
                            break;
                    }
            }
        }

        [Theory]
        [MemberData(nameof(EnvironmentNames))]
        public void AppSettings_ServicePrincipal(string environmentName, string overrideName)
        {
            var appSettings = LoadAppSettings(environmentName, overrideName);
            var secretProvider = new Mock<ISecretProvider>().Object;
            var servicePrincipal = new ServicePrincipal(
                appSettings.ApplicationServicePrincipal.ClientId, 
                appSettings.ApplicationServicePrincipal.ClientSecretName,
                appSettings.ApplicationServicePrincipal.TenantId,
                secretProvider);
            Assert.NotNull(servicePrincipal);
        }

        [Theory]
        [MemberData(nameof(EnvironmentNames))]
        public void AppSettings_ControlPlaneInfo(string environmentName, string overrideName)
        {
            var location = environmentName == "prod-can" ? AzureLocation.EastUs2Euap : AzureLocation.WestUs2;
            var controlPlaneInfo = LoadControlPlaneInfo(environmentName, overrideName, location);
            Assert.NotNull(controlPlaneInfo);
            var stamp = controlPlaneInfo.Stamp;
            Assert.NotNull(stamp);
            Assert.Contains(stamp.Location, stamp.DataPlaneLocations);
        }

        [Fact]
        public void AppSettings_ControlPlaneInfo_Development()
        {
            var controlPlaneInfo = LoadControlPlaneInfo("dev-ci", null, AzureLocation.WestUs2);

            void westUs2Stamp(IControlPlaneStampInfo s)
            {
                Assert.Equal(AzureLocation.WestUs2, s.Location);
                Assert.Equal("westus2-ci-online.dev.core.vsengsaas.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-dev-ci-usw2", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-dev-ci-usw2-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclk-online-dev-ci-usw2-service-bus", s.StampServiceBusNamespaceName);
                Assert.Equal("vsclkonlinedevciusw2sa", s.StampStorageAccountName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.WestUs2, l));
                Assert.Equal("vsodevciusw2cqusw2", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestUs2));
                Assert.Equal("vsodevciusw2vmusw2", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestUs2));
                Assert.Equal("vsodevciusw2siusw2", s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestUs2));
                Assert.Equal("vsodevciusw2bausw2", s.GetStampBatchAccountName(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampBatchAccountName(AzureLocation.EastUs));
            }

            Assert.Equal("online.dev.core.vsengsaas.visualstudio.com", controlPlaneInfo.DnsHostName);
            Assert.Equal("vsclk-online-dev", controlPlaneInfo.EnvironmentResourceGroupName);
            Assert.Equal("vsclk-online-dev-kv", controlPlaneInfo.EnvironmentKeyVaultName);
            Assert.Equal("vsclk-online-dev-ci", controlPlaneInfo.InstanceResourceGroupName);
            Assert.Equal("vsclk-online-dev-ci-db", controlPlaneInfo.GlobalCosmosDbAccountName);
            Assert.Equal("vsclk-online-dev-ci-usw2-regional-db", controlPlaneInfo.RegionalCosmosDbAccountName);
            Assert.Collection(controlPlaneInfo.Stamp.DataPlaneLocations,
                l => Assert.Equal(AzureLocation.WestUs2, l));
            westUs2Stamp(controlPlaneInfo.Stamp);
            Assert.False(controlPlaneInfo.TryGetSubscriptionId(out var _));
        }

        [Fact]
        public void AppSettings_ControlPlaneInfo_Staging()
        {
            var controlPlaneInfo = LoadControlPlaneInfo("ppe-rel", null, AzureLocation.WestUs2);

            void westUs2Stamp(IControlPlaneStampInfo s)
            {
                Assert.Equal(AzureLocation.WestUs2, s.Location);
                Assert.Equal("westus2-ppe-rel-online.core.vsengsaas.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-ppe-rel-usw2", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-ppe-rel-usw2-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclkonlinepperelusw2sa", s.StampStorageAccountName);
                Assert.Equal("vsclk-online-ppe-rel-usw2-service-bus", s.StampServiceBusNamespaceName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.WestUs2, l));
                Assert.Equal("vsopperelusw2cqusw2", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestUs2));
                Assert.Equal("vsopperelusw2vmusw2", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestUs2));
                Assert.Equal("vsopperelusw2siusw2", s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestUs2));
                Assert.Equal("vsopperelusw2bausw2", s.GetStampBatchAccountName(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampBatchAccountName(AzureLocation.EastUs));
            }

            void southEastAsiaStamp(IControlPlaneStampInfo s)
            {
                Assert.Equal(AzureLocation.SouthEastAsia, s.Location);
                Assert.Equal("southeastasia-ppe-rel-online.core.vsengsaas.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-ppe-rel-asse", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-ppe-rel-asse-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclk-online-ppe-rel-asse-service-bus", s.StampServiceBusNamespaceName);
                Assert.Equal("vsclkonlinepperelassesa", s.StampStorageAccountName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.SouthEastAsia, l));
                Assert.Equal("vsopperelassecqasse", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.SouthEastAsia));
                Assert.Equal("vsopperelassevmasse", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.SouthEastAsia));
                Assert.Equal("vsopperelassesiasse", s.GetStampStorageAccountNameForStorageImages(AzureLocation.SouthEastAsia));
                Assert.Equal("vsopperelassebaasse", s.GetStampBatchAccountName(AzureLocation.SouthEastAsia));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampBatchAccountName(AzureLocation.EastUs));
            }

            Assert.Collection(controlPlaneInfo.AllStamps.Values.OrderBy(s => s.Location.ToString()),
                southEastAsiaStamp,
                westUs2Stamp);
            Assert.Equal("online-ppe.core.vsengsaas.visualstudio.com", controlPlaneInfo.DnsHostName);
            Assert.Equal("vsclk-online-ppe", controlPlaneInfo.EnvironmentResourceGroupName);
            Assert.Equal("vsclk-online-ppe-kv", controlPlaneInfo.EnvironmentKeyVaultName);
            Assert.Equal("vsclk-online-ppe-rel", controlPlaneInfo.InstanceResourceGroupName);
            Assert.Equal("vsclk-online-ppe-rel-db", controlPlaneInfo.GlobalCosmosDbAccountName);
            Assert.Equal("vsclk-online-ppe-rel-usw2-regional-db", controlPlaneInfo.RegionalCosmosDbAccountName);
            Assert.Collection(controlPlaneInfo.Stamp.DataPlaneLocations,
                l => Assert.Equal(AzureLocation.WestUs2, l));
            westUs2Stamp(controlPlaneInfo.Stamp);
            Assert.False(controlPlaneInfo.TryGetSubscriptionId(out var _));
        }

        [Fact]
        public void AppSettings_ControlPlaneInfo_Production()
        {
            var controlPlaneInfo = LoadControlPlaneInfo("prod-rel", null, AzureLocation.WestUs2);

            void eastUsStamp(IControlPlaneStampInfo s)
            {
                Assert.Equal(AzureLocation.EastUs, s.Location);
                Assert.Equal("eastus.online.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-prod-rel-use", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-prod-rel-use-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclk-online-prod-rel-use-service-bus", s.StampServiceBusNamespaceName);
                Assert.Equal("vsclkonlineprodrelusesa", s.StampStorageAccountName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.EastUs, l));
                Assert.Equal("vsoprodrelusecquse", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.EastUs));
                Assert.Equal("vsoprodrelusevmuse", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.EastUs));
                Assert.Equal("vsoprodrelusesiuse", s.GetStampStorageAccountNameForStorageImages(AzureLocation.EastUs));
                Assert.Equal("vsoprodrelusebause", s.GetStampBatchAccountName(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampBatchAccountName(AzureLocation.WestUs2));
            }

            void westEuropeStamp(IControlPlaneStampInfo s)
            {
                Assert.Equal(AzureLocation.WestEurope, s.Location);
                Assert.Equal("westeurope.online.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-prod-rel-euw", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-prod-rel-euw-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclk-online-prod-rel-euw-service-bus", s.StampServiceBusNamespaceName);
                Assert.Equal("vsclkonlineprodreleuwsa", s.StampStorageAccountName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.WestEurope, l));
                Assert.Equal("vsoprodreleuwcqeuw", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestEurope));
                Assert.Equal("vsoprodreleuwvmeuw", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestEurope));
                Assert.Equal("vsoprodreleuwsieuw", s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestEurope));
                Assert.Equal("vsoprodreleuwbaeuw", s.GetStampBatchAccountName(AzureLocation.WestEurope));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampBatchAccountName(AzureLocation.EastUs));
            }

            void westUs2Stamp(IControlPlaneStampInfo s)
            {
                Assert.Equal(AzureLocation.WestUs2, s.Location);
                Assert.Equal("westus2.online.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-prod-rel-usw2", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-prod-rel-usw2-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclk-online-prod-rel-usw2-service-bus", s.StampServiceBusNamespaceName);
                Assert.Equal("vsclkonlineprodrelusw2sa", s.StampStorageAccountName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.WestUs2, l));
                Assert.Equal("vsoprodrelusw2cqusw2", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestUs2));
                Assert.Equal("vsoprodrelusw2vmusw2", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestUs2));
                Assert.Equal("vsoprodrelusw2siusw2", s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestUs2));
                Assert.Equal("vsoprodrelusw2bausw2", s.GetStampBatchAccountName(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestEurope));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestEurope));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestEurope));
                Assert.Throws<NotSupportedException>(() => s.GetStampBatchAccountName(AzureLocation.WestEurope));
            }

            void southEastAsiaStamp(IControlPlaneStampInfo s)
            {
                Assert.Equal(AzureLocation.SouthEastAsia, s.Location);
                Assert.Equal("southeastasia.online.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-prod-rel-asse", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-prod-rel-asse-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclk-online-prod-rel-asse-service-bus", s.StampServiceBusNamespaceName);
                Assert.Equal("vsclkonlineprodrelassesa", s.StampStorageAccountName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.SouthEastAsia, l));
                Assert.Equal("vsoprodrelassecqasse", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.SouthEastAsia));
                Assert.Equal("vsoprodrelassevmasse", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.SouthEastAsia));
                Assert.Equal("vsoprodrelassesiasse", s.GetStampStorageAccountNameForStorageImages(AzureLocation.SouthEastAsia));
                Assert.Equal("vsoprodrelassebaasse", s.GetStampBatchAccountName(AzureLocation.SouthEastAsia));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampBatchAccountName(AzureLocation.WestUs2));
            }

            Assert.Collection(controlPlaneInfo.AllStamps.Values.OrderBy(s => s.Location.ToString()),
                eastUsStamp,
                southEastAsiaStamp,
                westEuropeStamp,
                westUs2Stamp);
            Assert.Equal("online.visualstudio.com", controlPlaneInfo.DnsHostName);
            Assert.Equal("vsclk-online-prod", controlPlaneInfo.EnvironmentResourceGroupName);
            Assert.Equal("vsclk-online-prod-kv", controlPlaneInfo.EnvironmentKeyVaultName);
            Assert.Equal("vsclk-online-prod-rel", controlPlaneInfo.InstanceResourceGroupName);
            Assert.Equal("vsclk-online-prod-rel-db", controlPlaneInfo.GlobalCosmosDbAccountName);
            Assert.Equal("vsclk-online-prod-rel-usw2-regional-db", controlPlaneInfo.RegionalCosmosDbAccountName);
            Assert.Collection(controlPlaneInfo.Stamp.DataPlaneLocations,
                l => Assert.Equal(AzureLocation.WestUs2, l));
            westUs2Stamp(controlPlaneInfo.Stamp);
            Assert.False(controlPlaneInfo.TryGetSubscriptionId(out var _));
        }

        [Fact]
        public void AppSettings_ControlPlaneInfo_Production_Canary()
        {
            var controlPlaneInfo = LoadControlPlaneInfo("prod-can", null, AzureLocation.EastUs2Euap);

            void eastUs2EuapStamp(IControlPlaneStampInfo s)
            {
                Assert.Equal(AzureLocation.EastUs2Euap, s.Location);
                Assert.Equal("eastus2euap.online.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-prod-can-usec", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-prod-can-usec-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclk-online-prod-can-usec-service-bus", s.StampServiceBusNamespaceName);
                Assert.Equal("vsclkonlineprodcanusecsa", s.StampStorageAccountName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.EastUs2Euap, l));
                Assert.Equal("vsoprodcanuseccqusec", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.EastUs2Euap));
                Assert.Equal("vsoprodcanusecvmusec", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.EastUs2Euap));
                Assert.Equal("vsoprodcanusecsiusec", s.GetStampStorageAccountNameForStorageImages(AzureLocation.EastUs2Euap));
                Assert.Equal("vsoprodcanusecbausec", s.GetStampBatchAccountName(AzureLocation.EastUs2Euap));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampBatchAccountName(AzureLocation.WestUs2));
            }

            Assert.Collection(controlPlaneInfo.AllStamps.Values.OrderBy(s => s.Location.ToString()), eastUs2EuapStamp);

            Assert.Equal("canary.online.visualstudio.com", controlPlaneInfo.DnsHostName);
            Assert.Equal("vsclk-online-prod", controlPlaneInfo.EnvironmentResourceGroupName);
            Assert.Equal("vsclk-online-prod-kv", controlPlaneInfo.EnvironmentKeyVaultName);
            Assert.Equal("vsclk-online-prod-can", controlPlaneInfo.InstanceResourceGroupName);
            Assert.Equal("vsclk-online-prod-can-db", controlPlaneInfo.GlobalCosmosDbAccountName);
            Assert.Equal("vsclk-online-prod-can-usec-regional-db", controlPlaneInfo.RegionalCosmosDbAccountName);
            Assert.Collection(controlPlaneInfo.Stamp.DataPlaneLocations,
                l => Assert.Equal(AzureLocation.EastUs2Euap, l));
            eastUs2EuapStamp(controlPlaneInfo.Stamp);
            Assert.False(controlPlaneInfo.TryGetSubscriptionId(out var _));
        }

        public static AppSettingsBase LoadAppSettings(string environmentName, string overrideName = null)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.images.json", optional: false)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: false);

            if (!string.IsNullOrEmpty(overrideName))
            {
                builder.AddJsonFile($"appsettings.{overrideName}.json", optional: false);
            }

            var configuration = builder.Build();
            var appSettingsConfiguration = configuration.GetSection("AppSettings");
            var appSettings = appSettingsConfiguration.Get<AppSettingsBase>();
            return appSettings;
        }

        private static IControlPlaneInfo LoadControlPlaneInfo(
            string environmentName,
            string overrideName = null,
            AzureLocation location = AzureLocation.WestUs2)
        {
            var appSettings = LoadAppSettings(environmentName, overrideName);
            var currentLocationProvider = new Mock<ICurrentLocationProvider>();
            currentLocationProvider
                .Setup(obj => obj.CurrentLocation)
                .Returns(location);
            var controlPlaneOptions = new ControlPlaneInfoOptions
            {
                ControlPlaneSettings = appSettings.ControlPlaneSettings,
            };
            var devStampSettings = new DeveloperPersonalStampSettings(false, null, false);
            var resourceNameBuilder = new ResourceNameBuilder(devStampSettings);
            var controlPlaneInfo = new ControlPlaneInfo(Options.Create(controlPlaneOptions), currentLocationProvider.Object, resourceNameBuilder);
            return controlPlaneInfo;
        }

    }
}
