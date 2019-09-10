#if false
using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class ControlPlaneAzureResourceSettingsTests
    {
        private static readonly ControlPlaneSettings GoodSettings = new ControlPlaneSettings
        {
            Prefix = "pre",
            ServiceName = "srv",
            EnvironmentName = "env",
            InstanceName = "ins",
            DnsHostName = "dns-host-name",
            StampStorageAccountUniquePrefix = "uniqueprefix01",
            SubscriptionId = Guid.NewGuid().ToString(),
            Stamps =  
            {
                { 
                    AzureLocation.EastUs,
                    new ControlPlaneStampSettings
                    {
                        StampName = "stamp",
                        DnsHostName = "dns-host-name",
                        DataPlaneLocations =
                        {
                            AzureLocation.EastUs
                        }
                    }
                } 
            },
        };

        [Fact]
        public void GoodSettingsSerializationOK()
        {
            var settings = JsonConvert.SerializeObject(GoodSettings);
            Assert.NotNull(settings);
        }

        public static readonly TheoryData<string, string, string, string, string> BadSettings = new TheoryData<string, string, string, string, string>
        {
            { null, null, null, null, null },
            { null, "srv", "env", "ins", "stamp" },
            { "pre", null, "env", "ins", "stamp" },
            { "pre", "srv", null, "ins", "stamp" },
            { "pre", "srv", "env", null, "stamp" },
            { "pre", "srv", "env", "ins", null }
        };

        [Theory]
        [MemberData(nameof(BadSettings))]
        public void BadSettingsSerializationThrows(string prefix, string service, string environment, string instance, string stamp)
        {
            var badSettings = new ControlPlaneSettings
            {
                Prefix = prefix,
                ServiceName = service,
                EnvironmentName = environment,
                InstanceName = instance,
                StampName = stamp
            };

            Assert.Throws<JsonSerializationException> (() => _ = JsonConvert.SerializeObject(badSettings));
        }

        public static readonly TheoryData<string, string, string, string, string> BadSettings2 = new TheoryData<string, string, string, string, string>
        {
            { null, null, null, null, null },
            { null, "srv", "env", "ins", "stamp" },
            { "pre", null, "env", "ins", "stamp" },
            { "pre", "srv", null, "ins", "stamp" },
            { "pre", "srv", "env", null, "stamp" },
            { "pre", "srv", "env", "ins", null },
            { string.Empty, "srv", "env", "ins", "stamp" },
            { "pre", string.Empty, "env", "ins", "stamp" },
            { "pre", "srv", string.Empty, "ins", "stamp" },
            { "pre", "srv", "env", string.Empty, "stamp" },
            { "pre", "srv", "env", "ins", string.Empty}
        };

        [Theory]
        [MemberData(nameof(BadSettings))]
        public void BadSettingsPropertiesThrow(string prefix, string service, string environment, string instance, string stamp)
        {
            var badSettings = new ControlPlaneSettings
            {
                Prefix = prefix,
                ServiceName = service,
                EnvironmentName = environment,
                InstanceName = instance,
                StampName = stamp
            };

            Assert.Throws<InvalidOperationException>(() => _ = badSettings.StampResourceGroupName);
        }


        [Fact]
        public void GoodSettings_EnvironmentResourceGroupName()
        {
            Assert.Equal("pre-srv-env", GoodSettings.EnvironmentResourceGroupName);
        }

        [Fact]
        public void GoodSettings_EnvironmentKeyVaultName()
        {
            Assert.Equal("pre-srv-env-kv", GoodSettings.EnvironmentKeyVaultName);
        }

        [Fact]
        public void GoodSettings_InstanceResourceGroupName()
        {
            Assert.Equal("pre-srv-env-ins", GoodSettings.InstanceResourceGroupName);
        }

        [Fact]
        public void GoodSettings_InstanceCosmosDbAccountName()
        {
            Assert.Equal("pre-srv-env-ins-db", GoodSettings.InstanceCosmosDbAccountName);
        }

        [Fact]
        public void GoodSettings_StampResourceGroupName()
        {
            Assert.Equal("pre-srv-env-ins-stamp", GoodSettings.StampResourceGroupName);
        }

        [Fact]
        public void GoodSettings_StampCosmosDbAccountName()
        {
            Assert.Equal("pre-srv-env-ins-stamp-db", GoodSettings.StampCosmosDbAccountName);
        }

        [Fact]
        public void GoodSettings_StampStorageAccountName()
        {
            Assert.Equal("presrvenvinsstampsa", GoodSettings.StampStorageAccountName);
        }
    }
}

#endif