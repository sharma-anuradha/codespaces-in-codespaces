﻿using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
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
            { "dev-ci", null },
            { "dev-ci", "dev-stg" },
            { "dev-ci", "local" },
            { "ppe-rel", null },
            { "prod-rel", null },
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
            controlPlaneInfo.Setup(obj => obj.Stamp.DataPlaneLocations).Returns(new List<AzureLocation>());
            var controlPlaneAzureResourceAccessor = new Mock<IControlPlaneAzureResourceAccessor>().Object;
            var skuCatalog = new SkuCatalog(appSettings.SkuCatalogSettings, controlPlaneInfo.Object, controlPlaneAzureResourceAccessor);
            Assert.NotNull(skuCatalog);
            foreach (var item in skuCatalog.CloudEnvironmentSkus)
            {
                var name = item.Key;
                var sku = item.Value;
                Assert.Equal(name, sku.SkuName);
            }
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

            foreach (var subscription in subscriptionCatalog.AzureSubscriptions)
            {
                Assert.NotNull(subscription.ComputeQuotas);
                Assert.NotNull(subscription.NetworkQuotas);
                Assert.NotNull(subscription.StorageQuotas);
            }
        }

        [Theory]
        [MemberData(nameof(EnvironmentNames))]
        public void AppSettings_ServicePrincipal(string environmentName, string overrideName)
        {
            var appSettings = LoadAppSettings(environmentName, overrideName);
            var secretProvider = new Mock<ISecretProvider>().Object;
            var servicePrincipal = new ServicePrincipal(appSettings.ApplicationServicePrincipal, secretProvider);
            Assert.NotNull(servicePrincipal);
        }

        [Theory]
        [MemberData(nameof(EnvironmentNames))]
        public void AppSettings_ControlPlaneInfo(string environmentName, string overrideName)
        {
            var controlPlaneInfo = LoadControlPlaneInfo(environmentName, overrideName);
            Assert.NotNull(controlPlaneInfo);
            var stamp = controlPlaneInfo.Stamp;
            Assert.NotNull(stamp);
            Assert.Contains(stamp.Location, stamp.DataPlaneLocations);
        }

        [Fact]
        public void AppSettings_ControlPlaneInfo_Development()
        {
            var controlPlaneInfo = LoadControlPlaneInfo("dev-ci", null, AzureLocation.WestUs2);

            void westUs2Stamp(IControlPlaneStampInfo s) {
                Assert.Equal(AzureLocation.WestUs2, s.Location);
                Assert.Equal("westus2-ci-online.dev.core.vsengsaas.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-dev-ci-usw2", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-dev-ci-usw2-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclkonlinedevciusw2sa", s.StampStorageAccountName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.WestUs2, l));
                Assert.Equal("vsodevciusw2cqusw2", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestUs2));
                Assert.Equal("vsodevciusw2vmusw2", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestUs2));
                Assert.Equal("vsodevciusw2siusw2", s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.EastUs));
            }

            Assert.Collection(controlPlaneInfo.AllStamps.Values.OrderBy(s => s.Location.ToString()),
                westUs2Stamp);
            Assert.Equal("ci-online.dev.core.vsengsaas.visualstudio.com", controlPlaneInfo.DnsHostName);
            Assert.Equal("vsclk-online-dev", controlPlaneInfo.EnvironmentResourceGroupName);
            Assert.Equal("vsclk-online-dev-kv", controlPlaneInfo.EnvironmentKeyVaultName);
            Assert.Equal("vsclk-online-dev-ci", controlPlaneInfo.InstanceResourceGroupName);
            Assert.Equal("vsclk-online-dev-ci-db", controlPlaneInfo.InstanceCosmosDbAccountName);
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
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.WestUs2, l));
                Assert.Equal("vsopperelusw2cqusw2", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestUs2));
                Assert.Equal("vsopperelusw2vmusw2", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestUs2));
                Assert.Equal("vsopperelusw2siusw2", s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.EastUs));
            }

            void southEastAsiaStamp(IControlPlaneStampInfo s)
            {
                Assert.Equal(AzureLocation.SouthEastAsia, s.Location);
                Assert.Equal("southeastasia-ppe-rel-online.core.vsengsaas.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-ppe-rel-asse", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-ppe-rel-asse-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclkonlinepperelassesa", s.StampStorageAccountName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.SouthEastAsia, l));
                Assert.Equal("vsopperelassecqasse", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.SouthEastAsia));
                Assert.Equal("vsopperelassevmasse", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.SouthEastAsia));
                Assert.Equal("vsopperelassesiasse", s.GetStampStorageAccountNameForStorageImages(AzureLocation.SouthEastAsia));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.EastUs));
            }

            Assert.Collection(controlPlaneInfo.AllStamps.Values.OrderBy(s => s.Location.ToString()),
                southEastAsiaStamp,
                westUs2Stamp);
            Assert.Equal("online-ppe.core.vsengsaas.visualstudio.com", controlPlaneInfo.DnsHostName);
            Assert.Equal("vsclk-online-ppe", controlPlaneInfo.EnvironmentResourceGroupName);
            Assert.Equal("vsclk-online-ppe-kv", controlPlaneInfo.EnvironmentKeyVaultName);
            Assert.Equal("vsclk-online-ppe-rel", controlPlaneInfo.InstanceResourceGroupName);
            Assert.Equal("vsclk-online-ppe-rel-db", controlPlaneInfo.InstanceCosmosDbAccountName);
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
                Assert.Equal("eastus-prod-rel.online.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-prod-rel-use", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-prod-rel-use-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclkonlineprodrelusesa", s.StampStorageAccountName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.EastUs, l));
                Assert.Equal("vsoprodrelusecquse", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.EastUs));
                Assert.Equal("vsoprodrelusevmuse", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.EastUs));
                Assert.Equal("vsoprodrelusesiuse", s.GetStampStorageAccountNameForStorageImages(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestUs2));
            }

            void westEuropeStamp(IControlPlaneStampInfo s)
            {
                Assert.Equal(AzureLocation.WestEurope, s.Location);
                Assert.Equal("westeurope-prod-rel.online.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-prod-rel-euw", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-prod-rel-euw-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclkonlineprodreleuwsa", s.StampStorageAccountName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.WestEurope, l));
                Assert.Equal("vsoprodreleuwcqeuw", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestEurope));
                Assert.Equal("vsoprodreleuwvmeuw", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestEurope));
                Assert.Equal("vsoprodreleuwsieuw", s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestEurope));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.EastUs));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.EastUs));
            }

            void westUs2Stamp(IControlPlaneStampInfo s)
            {
                Assert.Equal(AzureLocation.WestUs2, s.Location);
                Assert.Equal("westus2-prod-rel.online.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-prod-rel-usw2", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-prod-rel-usw2-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclkonlineprodrelusw2sa", s.StampStorageAccountName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.WestUs2, l));
                Assert.Equal("vsoprodrelusw2cqusw2", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestUs2));
                Assert.Equal("vsoprodrelusw2vmusw2", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestUs2));
                Assert.Equal("vsoprodrelusw2siusw2", s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestEurope));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestEurope));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestEurope));
            }

            void southEastAsiaStamp(IControlPlaneStampInfo s)
            {
                Assert.Equal(AzureLocation.SouthEastAsia, s.Location);
                Assert.Equal("southeastasia-prod-rel.online.visualstudio.com", s.DnsHostName);
                Assert.Equal("vsclk-online-prod-rel-asse", s.StampResourceGroupName);
                Assert.Equal("vsclk-online-prod-rel-asse-db", s.StampCosmosDbAccountName);
                Assert.Equal("vsclkonlineprodrelassesa", s.StampStorageAccountName);
                Assert.Collection(s.DataPlaneLocations,
                    l => Assert.Equal(AzureLocation.SouthEastAsia, l));
                Assert.Equal("vsoprodrelassecqasse", s.GetStampStorageAccountNameForComputeQueues(AzureLocation.SouthEastAsia));
                Assert.Equal("vsoprodrelassevmasse", s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.SouthEastAsia));
                Assert.Equal("vsoprodrelassesiasse", s.GetStampStorageAccountNameForStorageImages(AzureLocation.SouthEastAsia));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeQueues(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation.WestUs2));
                Assert.Throws<NotSupportedException>(() => s.GetStampStorageAccountNameForStorageImages(AzureLocation.WestUs2));
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
            Assert.Equal("vsclk-online-prod-rel-db", controlPlaneInfo.InstanceCosmosDbAccountName);
            Assert.Collection(controlPlaneInfo.Stamp.DataPlaneLocations,
                l => Assert.Equal(AzureLocation.WestUs2, l));
            westUs2Stamp(controlPlaneInfo.Stamp);
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
            var controlPlaneInfo = new ControlPlaneInfo(Options.Create(controlPlaneOptions), currentLocationProvider.Object);
            return controlPlaneInfo;
        }

    }
}
