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
                    PortForwardingDomainTemplate = "{0}.fake.vso.io",
                    GitHubPortForwardingDomainTemplate = "{0}.apps.test.workspaces.githubusercontent.com",
                    PortForwardingHostsConfigs = new[]
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
                                "{0}.app.online.visualstudio.com",
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

