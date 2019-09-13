using System;
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
            var appSettings = LoadAppSettings(environmentName, overrideName);
            var currentLocationProvider = new Mock<ICurrentLocationProvider>();
            currentLocationProvider
                .Setup(obj => obj.CurrentLocation)
                .Returns(AzureLocation.WestUs2);
            var controlPlaneOptions = new ControlPlaneInfoOptions
            {
                ControlPlaneSettings = appSettings.ControlPlaneSettings,
            };
            var controlPlaneInfo = new ControlPlaneInfo(Options.Create(controlPlaneOptions), currentLocationProvider.Object);
            Assert.NotNull(controlPlaneInfo);
            var stamp = controlPlaneInfo.Stamp;
            Assert.NotNull(stamp);
            Assert.Contains(stamp.Location, stamp.DataPlaneLocations);
        }

        public static AppSettingsBase LoadAppSettings(string environmentName, string overrideName)
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
    }
}
