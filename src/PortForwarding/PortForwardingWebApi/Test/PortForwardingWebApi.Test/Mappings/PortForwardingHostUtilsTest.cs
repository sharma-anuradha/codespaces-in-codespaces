using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common;
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
            { "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec-8080.app.vso.io", false },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.apps.github.com", true },
            { "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec-8080.apps.github.com", true },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.invalid.host.dev", false },
            { "invalidWorkspace-8080.app.vso.io", false },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-abc.app.vso.io", false },
        };

        public static TheoryData<string, bool, (string WorkspaceId, string EnvironmentId, int Port)> TestParsedHosts = new TheoryData<string, bool, (string WorkspaceId, string EnvironmentId, int Port)>
        {
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.app.vso.io", true, ("a68c43fa9e015e45e046c85d502ec5e4b774", default, 8080) },
            { "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec-8080.app.vso.io", false, default },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.apps.github.com", true, ("a68c43fa9e015e45e046c85d502ec5e4b774", default, 8080) },
            { "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec-8080.apps.github.com", true, (default, "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec", 8080) },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.invalid.host.dev", false, default },
            { "invalidWorkspace-8080.app.vso.io", false, default },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-abc.app.vso.io", false, default },
        };

        public static TheoryData<string, bool, (string WorkspaceId, string EnvrionmentId, int Port)> TestUrls = new TheoryData<string, bool, (string WorkspaceId, string EnvrionmentId, int Port)>
        {
            { "https://a68c43fa9e015e45e046c85d502ec5e4b774-8080.app.vso.io/", true, ("a68c43fa9e015e45e046c85d502ec5e4b774", default, 8080) },
            { "https://9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec-8080.app.vso.io/", false, default },
            { "https://a68c43fa9e015e45e046c85d502ec5e4b774-8080.apps.github.com/stuff", true, ("a68c43fa9e015e45e046c85d502ec5e4b774", default, 8080) },
            { "https://9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec-8080.apps.github.com/stuff", true, (default, "9a1137fc-f6bd-4f08-9a6a-bb39efc8d3ec", 8080) },
            { "https://a68c43fa9e015e45e046c85d502ec5e4b774-8080.invalid.host.dev", false, default },
            { "https://invalidWorkspace-8080.app.vso.io/random.file", false, default },
            { "https://a68c43fa9e015e45e046c85d502ec5e4b774-abc.app.vso.io", false, default },
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
        public void PortForwardingHostUtils_TryGetPortForwardingSessionDetails(string host, bool isValid, (string, string, int) expectedResult)
        {
            var utils = new PortForwardingHostUtils(settings.HostsConfigs);
            Assert.Equal(isValid, utils.TryGetPortForwardingSessionDetails(host, out var sessionDetails));
            Assert.Equal(expectedResult, sessionDetails);
        }

        [Theory]
        [MemberData(nameof(TestUrls))]
        public void PortForwardingHostUtils_TryGetPortForwardingSessionDetails_FromRequest(string url, bool isValid, (string, string, int) expectedResult)
        {
            var utils = new PortForwardingHostUtils(settings.HostsConfigs);

            var context = MockHttpContext.Create();
            context.Request.Headers.Add(PortForwardingHeaders.OriginalUrl, url);

            Assert.Equal(isValid, utils.TryGetPortForwardingSessionDetails(context.Request, out var sessionDetails));
            Assert.Equal(expectedResult, sessionDetails);
        }
    }
}