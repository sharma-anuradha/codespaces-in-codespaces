using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test.Configuration.KeyGenerator
{
    public class ConfigurationKeyGeneratorTests
    {
        [Fact]
        public void TestDefaultConfigurationContext()
        {
            var configurationScopeGenerator = CreateConfigurationScopeGenerator();

            // default context
            var context = ConfigurationContextBuilder.GetDefaultContext();

            var scopes = configurationScopeGenerator.GetScopes(context);

            Assert.Equal(2, scopes.Count());
            // The first one should be most specific i.e. highest priority key. In default context, the regions scope key should be first then
            Assert.Equal("vsclk-usw2", scopes.First());
            Assert.Equal("feature:vsclk-usw2:vnet-injection-enabled", ConfigurationHelpers.GetCompleteKey(scopes.First(), ConfigurationType.Feature, "vnet-injection", ConfigurationConstants.EnabledFeatureName));

            // The last or 2nd should be the service scoped key
            Assert.Equal("vsclk", scopes.Last());
            Assert.Equal("feature:vsclk:vnet-injection-enabled", ConfigurationHelpers.GetCompleteKey(scopes.Last(), ConfigurationType.Feature, "vnet-injection", ConfigurationConstants.EnabledFeatureName));
        }

        [Fact]
        public void TestSubscriptionConfigurationContextWithRegionAndServiceScopeApplicable()
        {
            var configurationScopeGenerator = CreateConfigurationScopeGenerator();

            // context with subscription scope. By default region and service scope are applicable
            var subId = "1234-5678-909";
            var context = ConfigurationContextBuilder.GetSubscriptionContext(subId);

            var subScopes = configurationScopeGenerator.GetScopes(context);

            // we should get 4 scopes - 1 for service scope, 1 for region scope, and 2 for subscription scope (one for subscription at service scope and other for subscription under a region)
            Assert.Equal(4, subScopes.Count());

            // The first one should be most specific i.e. highest priority key. In this context, the subscription scoped key under a region should be first.
            Assert.Equal("vsclk-usw2-subscription-1234-5678-909", subScopes.ElementAt(0));
            Assert.Equal("quota:vsclk-usw2-subscription-1234-5678-909:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(subScopes.ElementAt(0), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            // The 2nd should be the subscription scoped key at service level.
            Assert.Equal("vsclk-subscription-1234-5678-909", subScopes.ElementAt(1));
            Assert.Equal("quota:vsclk-subscription-1234-5678-909:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(subScopes.ElementAt(1), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            // The 3rd should be the the region scoped key.
            Assert.Equal("vsclk-usw2", subScopes.ElementAt(2));
            Assert.Equal("quota:vsclk-usw2:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(subScopes.ElementAt(2), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            // The last should be service scoped key. This is also the one with least priority
            Assert.Equal("vsclk", subScopes.ElementAt(3));
            Assert.Equal("quota:vsclk:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(subScopes.ElementAt(3), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));
        }

        [Fact]
        public void TestKeyGenerationWithCompletelyFilledContext()
        {
            // Completely filled context means all the scopes are applicable

            var configurationScopeGenerator = CreateConfigurationScopeGenerator();
            var subId = "1234-5678-909";
            var planName = "xyz";
            var userId = "uuuu";
            // context with plan scope. We can also get a default context and set properties appropriately.
            var context = ConfigurationContextBuilder.GetPlanContext(subId, planName);
            context.UserId = userId;


            var planScopes = configurationScopeGenerator.GetScopes(context);

            // 2 from user scope, 2 from plan, 2 from subscription scope and one each from region and service scope.
            Assert.Equal(8, planScopes.Count());

            // 2 user scopes
            Assert.Equal("vsclk-usw2-user-uuuu", planScopes.ElementAt(0));
            Assert.Equal("quota:vsclk-usw2-user-uuuu:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(planScopes.ElementAt(0), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            Assert.Equal("vsclk-user-uuuu", planScopes.ElementAt(1));
            Assert.Equal("quota:vsclk-user-uuuu:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(planScopes.ElementAt(1), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            // next 2 should be plan scope
            Assert.Equal("vsclk-usw2-subscription-1234-5678-909-plan-xyz", planScopes.ElementAt(2));
            Assert.Equal("quota:vsclk-usw2-subscription-1234-5678-909-plan-xyz:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(planScopes.ElementAt(2), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            Assert.Equal("vsclk-subscription-1234-5678-909-plan-xyz", planScopes.ElementAt(3));
            Assert.Equal("quota:vsclk-subscription-1234-5678-909-plan-xyz:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(planScopes.ElementAt(3), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            // next 2 are subscription scopes
            Assert.Equal("vsclk-usw2-subscription-1234-5678-909", planScopes.ElementAt(4));
            Assert.Equal("quota:vsclk-usw2-subscription-1234-5678-909:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(planScopes.ElementAt(4), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            Assert.Equal("vsclk-subscription-1234-5678-909", planScopes.ElementAt(5));
            Assert.Equal("quota:vsclk-subscription-1234-5678-909:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(planScopes.ElementAt(5), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            // last 2 are region and service scope keys respectively
            Assert.Equal("vsclk-usw2", planScopes.ElementAt(6));
            Assert.Equal("quota:vsclk-usw2:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(planScopes.ElementAt(6), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            Assert.Equal("vsclk", planScopes.ElementAt(7));
            Assert.Equal("quota:vsclk:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(planScopes.ElementAt(7), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));
        }

        // since a plan is associated with a subscription, the subscription scope applies as well. We cannot switch it off.
        [Fact]
        public void TestPlanScopeKeyGeneration()
        {
            var configurationScopeGenerator = CreateConfigurationScopeGenerator();
            var subId = "1234-5678-909";
            var planName = "xyz";

            // context with only plan scope
            var context = ConfigurationContextBuilder.GetPlanContext(subId, planName);
            context.ServiceScopeApplicable = false;
            context.RegionScopeApplicable = false;

            var planScopes = configurationScopeGenerator.GetScopes(context);

            // 2 from plan and 2 from subscription scope. Plan scopes should have higher priority.
            Assert.Equal(4, planScopes.Count());
            Assert.Equal("vsclk-usw2-subscription-1234-5678-909-plan-xyz", planScopes.ElementAt(0));
            Assert.Equal("quota:vsclk-usw2-subscription-1234-5678-909-plan-xyz:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(planScopes.ElementAt(0), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            Assert.Equal("vsclk-subscription-1234-5678-909-plan-xyz", planScopes.ElementAt(1));
            Assert.Equal("quota:vsclk-subscription-1234-5678-909-plan-xyz:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(planScopes.ElementAt(1), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            Assert.Equal("vsclk-usw2-subscription-1234-5678-909", planScopes.ElementAt(2));
            Assert.Equal("quota:vsclk-usw2-subscription-1234-5678-909:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(planScopes.ElementAt(2), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            Assert.Equal("vsclk-subscription-1234-5678-909", planScopes.ElementAt(3));
            Assert.Equal("quota:vsclk-subscription-1234-5678-909:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(planScopes.ElementAt(3), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));
        }

        [Fact]
        public void TestServiceScopeKeyGeneration()
        {
            var configurationScopeGenerator = CreateConfigurationScopeGenerator();
            // context with only service scope
            var context = ConfigurationContextBuilder.GetDefaultContext();
            context.RegionScopeApplicable = false;
            var serviceScope = configurationScopeGenerator.GetScopes(context);

            Assert.Single(serviceScope);
            Assert.Equal("vsclk", serviceScope.First());
            Assert.Equal("feature:vsclk:vnet-injection-enabled", ConfigurationHelpers.GetCompleteKey(serviceScope.First(), ConfigurationType.Feature, "vnet-injection", ConfigurationConstants.EnabledFeatureName));
        }

        [Fact]
        public void TestRegionScopeKeyGeneration()
        {
            var configurationScopeGenerator = CreateConfigurationScopeGenerator();
            // context with only region scope
            var context = ConfigurationContextBuilder.GetDefaultContext();
            context.ServiceScopeApplicable = false;
            var regionScope = configurationScopeGenerator.GetScopes(context);

            Assert.Single(regionScope);
            Assert.Equal("vsclk-usw2", regionScope.First());
            Assert.Equal("setting:vsclk-usw2:watchpooljob-enabled", ConfigurationHelpers.GetCompleteKey(regionScope.First(), ConfigurationType.Setting, "watchpooljob", ConfigurationConstants.EnabledSettingName));
        }

        [Fact]
        public void TestSubsciptionScopeKeyGeneration()
        {
            var configurationScopeGenerator = CreateConfigurationScopeGenerator();

            // context with only subscription scope
            var subId = "1234-5678-909";
            var context = ConfigurationContextBuilder.GetSubscriptionContext(subId);
            context.ServiceScopeApplicable = false;
            context.RegionScopeApplicable = false;

            var subScopes = configurationScopeGenerator.GetScopes(context);

            Assert.Equal(2, subScopes.Count());
            Assert.Equal("vsclk-usw2-subscription-1234-5678-909", subScopes.First());
            Assert.Equal("quota:vsclk-usw2-subscription-1234-5678-909:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(subScopes.First(), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));

            Assert.Equal("vsclk-subscription-1234-5678-909", subScopes.Last());
            Assert.Equal("quota:vsclk-subscription-1234-5678-909:environmentmanager-max-plans-per-sub", ConfigurationHelpers.GetCompleteKey(subScopes.Last(), ConfigurationType.Quota, ConfigurationConstants.EnvironmentManagerComponent, "max-plans-per-sub"));
        }

        [Fact]
        public void TestUserScopeKeyGeneration()
        {
            var configurationScopeGenerator = CreateConfigurationScopeGenerator();

            // context with only user scope
            var userId = "1234-5678-909";
            var context = ConfigurationContextBuilder.GetUserContext(userId);
            context.ServiceScopeApplicable = false;
            context.RegionScopeApplicable = false;

            var configurationKeyGenerator = CreateConfigurationScopeGenerator();

            var userScopes = configurationScopeGenerator.GetScopes(context);
            
            Assert.Equal(2, userScopes.Count());
            Assert.Equal("vsclk-usw2-user-1234-5678-909", userScopes.First());
            Assert.Equal("quota:vsclk-usw2-user-1234-5678-909:planmanager-max-plans-per-user", ConfigurationHelpers.GetCompleteKey(userScopes.First(), ConfigurationType.Quota, ConfigurationConstants.PlanManagerComponent, "max-plans-per-user"));

            Assert.Equal("vsclk-user-1234-5678-909", userScopes.Last());
            Assert.Equal("quota:vsclk-user-1234-5678-909:planmanager-max-plans-per-user", ConfigurationHelpers.GetCompleteKey(userScopes.Last(), ConfigurationType.Quota, ConfigurationConstants.PlanManagerComponent, "max-plans-per-user"));
        }

        private ConfigurationScopeGenerator CreateConfigurationScopeGenerator()
        {
            var (controlPlaneOptions, controlPlaneInfo) = LoadControlPlaneInfo("prod-rel", null, AzureLocation.WestUs2);

            var options = new Mock<IOptions<ControlPlaneInfoOptions>>();
            options.Setup(t => t.Value).Returns(controlPlaneOptions);

            return new ConfigurationScopeGenerator(options.Object, controlPlaneInfo);
        }

        private AppSettingsBase LoadAppSettings(string environmentName, string overrideName = null)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.images.json", optional: false)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: false)
                .AddJsonFile($"appsettings.subscriptions.{environmentName}.jsonc", optional: true);

            if (!string.IsNullOrEmpty(overrideName))
            {
                builder.AddJsonFile($"appsettings.{overrideName}.json", optional: false);
                builder.AddJsonFile($"appsettings.subscriptions.{overrideName}.jsonc", optional: true);
            }

            var configuration = builder.Build();
            var appSettingsConfiguration = configuration.GetSection("AppSettings");
            var appSettings = appSettingsConfiguration.Get<AppSettingsBase>();
            return appSettings;
        }

        private (ControlPlaneInfoOptions, IControlPlaneInfo) LoadControlPlaneInfo(
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
            return (controlPlaneOptions, controlPlaneInfo);
        }
    }
}
