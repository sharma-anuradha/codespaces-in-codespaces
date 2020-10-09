using Microsoft.VsCloudKernel.Services.Portal.WebSite;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortalWebsite.Test
{
    public static class MockAppSettings
    {
        public static AppSettings Settings
        {
            get
            {
                return new AppSettings
                {
                    AesIV = "00000000000000000000000000000000",
                    AesKey = "00000000000000000000000000000000",
                    PortalEndpoint = "https://fake.portal.dev",
                    PortForwardingDomainTemplate = "{0}.app.online.visualstudio.com",
                    GitHubPortForwardingDomainTemplate = "{0}.apps.test.codespaces.githubusercontent.com",
                    PortForwardingHostsConfigs = new[]
                    {
                        new HostsConfig
                        {
                            Hosts = new[]
                            {
                                "{0}.app.online.visualstudio.com",
                            },
                        },
                        new HostsConfig
                        {
                            Hosts = new[]
                            {
                                "{0}.apps.test.codespaces.githubusercontent.com",
                            },
                        },
                    },
                };
            }
        }
    }
}

