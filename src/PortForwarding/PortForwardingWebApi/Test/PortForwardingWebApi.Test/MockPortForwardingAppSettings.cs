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
                    HostsConfigs = new[]
                    {
                        new HostsConfig
                        {
                            Hosts = new[]
                            {
                                "{0}.app.vso.io"
                            }
                        },
                        new HostsConfig
                        {
                            Hosts = new[]
                            {
                                "{0}.apps.github.com"
                            }
                        }
                    }
                };
            }
        }
    }
}

