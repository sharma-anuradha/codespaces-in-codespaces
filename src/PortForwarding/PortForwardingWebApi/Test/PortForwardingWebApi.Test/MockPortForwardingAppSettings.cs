using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Test
{
    public static class MockPortForwardingAppSettings
    {
        public static PortForwardingAppSettings Settings
        {
            get
            {
                return new PortForwardingAppSettings
                {
                    VSLiveShareApiEndpoint = "https://fake.liveshare.dev/",
                    HostsConfigs = new[]
                    {
                        new HostsConfig
                        {
                            AllowEnvironmentIdBasedHosts = false,
                            Hosts = new[]
                            {
                                "{0}.app.vso.io",
                            },
                        },
                        new HostsConfig
                        {
                            AllowEnvironmentIdBasedHosts = true,
                            Hosts = new[]
                            {
                                "{0}.apps.github.com",
                            },
                        },
                    },
                };
            }
        }
    }
}

