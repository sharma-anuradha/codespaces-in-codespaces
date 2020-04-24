using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Models;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Test.Mappings
{
    public class PortForwardingHostUtilsTest
    {
        private readonly PortForwardingAppSettings settings;

        public PortForwardingHostUtilsTest()
        {
            settings = MockPortForwardingAppSettings.Settings;
        }

        public static TheoryData<string, bool> TestHosts = new TheoryData<string, bool>
        {
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.app.vso.io", true },
            { "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec-8080.app.vso.io", true },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.apps.github.com", true },
            { "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec-8080.apps.github.com", true },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.invalid.host.dev", false },
            { "invalidWorkspace-8080.app.vso.io", false },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-abc.app.vso.io", false },
        };

        public static TheoryData<string, bool, PortForwardingSessionDetails> TestParsedHosts = new TheoryData<string, bool, PortForwardingSessionDetails>
        {
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.app.vso.io", true, new WorkspaceSessionDetails("a68c43fa9e015e45e046c85d502ec5e4b774", 8080) },
            { "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec-8080.app.vso.io", true, new PartialEnvironmentSessionDetails("9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec", 8080) },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.apps.github.com", true, new WorkspaceSessionDetails("a68c43fa9e015e45e046c85d502ec5e4b774", 8080) },
            { "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec-8080.apps.github.com", true, new PartialEnvironmentSessionDetails("9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec", 8080) },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.invalid.host.dev", false, default },
            { "invalidWorkspace-8080.app.vso.io", false, default },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-abc.app.vso.io", false, default },
        };

        public static TheoryData<string, bool, PortForwardingSessionDetails> TestUrls = new TheoryData<string, bool, PortForwardingSessionDetails>
        {
            { "https://a68c43fa9e015e45e046c85d502ec5e4b774-8080.app.vso.io/", true, new WorkspaceSessionDetails("a68c43fa9e015e45e046c85d502ec5e4b774", 8080) },
            { "https://9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec-8080.app.vso.io/", true, new PartialEnvironmentSessionDetails("9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec", 8080) },
            { "https://a68c43fa9e015e45e046c85d502ec5e4b774-8080.apps.github.com/stuff", true, new WorkspaceSessionDetails("a68c43fa9e015e45e046c85d502ec5e4b774", 8080) },
            { "https://9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec-8080.apps.github.com/stuff", true, new PartialEnvironmentSessionDetails("9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec", 8080) },
            { "https://a68c43fa9e015e45e046c85d502ec5e4b774-8080.invalid.host.dev", false, default },
            { "https://invalidWorkspace-8080.app.vso.io/random.file", false, default },
            { "https://a68c43fa9e015e45e046c85d502ec5e4b774-abc.app.vso.io", false, default },
        };

        public static TheoryData<string, string, string, bool, PortForwardingSessionDetails> HeaderValues = new TheoryData<string, string, string, bool, PortForwardingSessionDetails>
        {
            { "a68c43fa9e015e45e046c85d502ec5e4b774", null, "8080", true, new WorkspaceSessionDetails("a68c43fa9e015e45e046c85d502ec5e4b774", 8080) },
            { "a68c43fa9e015e45e046c85d502ec5e4b774", "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec", "8080", true, new EnvironmentSessionDetails("a68c43fa9e015e45e046c85d502ec5e4b774", "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec", 8080) },
            { null, "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec", "8080", false, default },
            { "invalidWorkspace", null, "8080", false, default },
            { "invalidWorkspace", null, null, false, default },
            { "a68c43fa9e015e45e046c85d502ec5e4b774", null, "abc", false, default },
        };

        [Theory]
        [MemberData(nameof(TestHosts))]
        public void PortForwardingHostUtils_IsValidHost(string host, bool isValid)
        {
            var utils = new PortForwardingHostUtils(settings.HostsConfigs);
            Assert.Equal(isValid, utils.IsPortForwardingHost(host));
        }

        [Theory]
        [MemberData(nameof(TestParsedHosts))]
        public void PortForwardingHostUtils_TryGetPortForwardingSessionDetails(string host, bool isValid, PortForwardingSessionDetails expectedResult)
        {
            var utils = new PortForwardingHostUtils(settings.HostsConfigs);
            Assert.Equal(isValid, utils.TryGetPortForwardingSessionDetails(host, out var sessionDetails));
            Assert.Equal(expectedResult, sessionDetails);
        }

        [Theory]
        [MemberData(nameof(TestUrls))]
        public void PortForwardingHostUtils_TryGetPortForwardingSessionDetails_FromRequest(string url, bool isValid, PortForwardingSessionDetails expectedResult)
        {
            var utils = new PortForwardingHostUtils(settings.HostsConfigs);

            var context = MockHttpContext.Create();
            context.Request.Headers.Add(PortForwardingHeaders.OriginalUrl, url);

            Assert.Equal(isValid, utils.TryGetPortForwardingSessionDetails(context.Request, out var sessionDetails));
            Assert.Equal(expectedResult, sessionDetails);
        }

        [Theory]
        [MemberData(nameof(HeaderValues))]
        public void PortForwardingHostUtils_Headers(string workspaceId, string environmentId, string port, bool result, PortForwardingSessionDetails expectedDetails)
        {
            var utils = new PortForwardingHostUtils(settings.HostsConfigs);

            var context = MockHttpContext.Create();
            if (workspaceId != null)
            {
                context.Request.Headers.Add(PortForwardingHeaders.WorkspaceId, workspaceId);
            }
            if (environmentId != null)
            {
                context.Request.Headers.Add(PortForwardingHeaders.EnvironmentId, environmentId);
            }
            if (port != null)
            {
                context.Request.Headers.Add(PortForwardingHeaders.Port, port);
            }

            Assert.Equal(result, utils.TryGetPortForwardingSessionDetails(context.Request, out var sessionDetails));
            Assert.Equal(expectedDetails, sessionDetails);
        }

        [Fact]
        public void PortForwardingHostUtils_WorkspaceId_HeadersAndHost()
        {
            var utils = new PortForwardingHostUtils(settings.HostsConfigs);

            var context = MockHttpContext.Create();
            context.Request.Host = HostString.FromUriComponent("a68c43fa9e015e45e046c85d502ec5e4b774-8080.app.vso.io");
            context.Request.Headers.Add(PortForwardingHeaders.WorkspaceId, "a68c43fa9e015e45e046c85d502ec5e4b774");
            context.Request.Headers.Add(PortForwardingHeaders.Port, "8080");

            Assert.True(utils.TryGetPortForwardingSessionDetails(context.Request, out var sessionDetails));
            Assert.Equal(new WorkspaceSessionDetails("a68c43fa9e015e45e046c85d502ec5e4b774", 8080), sessionDetails);
        }

        [Fact]
        public void PortForwardingHostUtils_WorkspaceId_HeadersAndOriginalUrl()
        {
            var utils = new PortForwardingHostUtils(settings.HostsConfigs);

            var context = MockHttpContext.Create();
            context.Request.Headers.Add(PortForwardingHeaders.OriginalUrl, "a68c43fa9e015e45e046c85d502ec5e4b774-8080.app.vso.io");
            context.Request.Headers.Add(PortForwardingHeaders.WorkspaceId, "a68c43fa9e015e45e046c85d502ec5e4b774");
            context.Request.Headers.Add(PortForwardingHeaders.Port, "8080");

            Assert.True(utils.TryGetPortForwardingSessionDetails(context.Request, out var sessionDetails));
            Assert.Equal(new WorkspaceSessionDetails("a68c43fa9e015e45e046c85d502ec5e4b774", 8080), sessionDetails);
        }

        [Fact]
        public void PortForwardingHostUtils_EnvironmentHostMustMatchHeaderHost_False()
        {
            var utils = new PortForwardingHostUtils(settings.HostsConfigs);

            var context = MockHttpContext.Create();
            context.Request.Host = HostString.FromUriComponent("a68c43fa9e015e45e046c85d502ec5e4b774-8080.app.vso.io");
            context.Request.Headers.Add(PortForwardingHeaders.WorkspaceId, "a68c43fa9e015e45e046c85d502ec5e4b774");
            context.Request.Headers.Add(PortForwardingHeaders.EnvironmentId, "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec");
            context.Request.Headers.Add(PortForwardingHeaders.Port, "8080");

            Assert.False(utils.TryGetPortForwardingSessionDetails(context.Request, out var sessionDetails));
            Assert.Equal(default, sessionDetails);
        }

        [Fact]
        public void PortForwardingHostUtils_EnvironmentHostMustMatchHeaderHost_True()
        {
            var utils = new PortForwardingHostUtils(settings.HostsConfigs);

            var context = MockHttpContext.Create();
            context.Request.Host = HostString.FromUriComponent("9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec-8080.app.vso.io");
            context.Request.Headers.Add(PortForwardingHeaders.WorkspaceId, "a68c43fa9e015e45e046c85d502ec5e4b774");
            context.Request.Headers.Add(PortForwardingHeaders.EnvironmentId, "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec");
            context.Request.Headers.Add(PortForwardingHeaders.Port, "8080");

            Assert.True(utils.TryGetPortForwardingSessionDetails(context.Request, out var sessionDetails));
            Assert.Equal(new EnvironmentSessionDetails("a68c43fa9e015e45e046c85d502ec5e4b774", "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec", 8080), sessionDetails);
        }

        [Fact]
        public void PortForwardingHostUtils_HeadersAreEnough()
        {
            var utils = new PortForwardingHostUtils(settings.HostsConfigs);

            var context = MockHttpContext.Create();
            context.Request.Host = HostString.FromUriComponent("portal.default.svc");
            context.Request.Headers.Add(PortForwardingHeaders.Token, "super_secret_token");
            context.Request.Headers.Add(PortForwardingHeaders.WorkspaceId, "a68c43fa9e015e45e046c85d502ec5e4b774");
            context.Request.Headers.Add(PortForwardingHeaders.EnvironmentId, "0b125897-9e5c-438f-b286-a795f0419c3b");
            context.Request.Headers.Add(PortForwardingHeaders.Port, "8080");

            Assert.True(utils.TryGetPortForwardingSessionDetails(context.Request, out var sessionDetails));
            Assert.Equal(new EnvironmentSessionDetails("a68c43fa9e015e45e046c85d502ec5e4b774", "0b125897-9e5c-438f-b286-a795f0419c3b", 8080), sessionDetails);
        }
    }
}