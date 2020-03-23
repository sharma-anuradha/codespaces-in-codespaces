using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings;
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
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.apps.github.com", true },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.invalid.host.dev", false },
            { "invalidWorkspace-8080.app.vso.io", false },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-abc.app.vso.io", false },
        };

        public static TheoryData<string, bool, (string WorkspaceId, int Port)> TestParsedHosts = new TheoryData<string, bool,  (string WorkspaceId, int Port)>
        {
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.app.vso.io", true, ("a68c43fa9e015e45e046c85d502ec5e4b774", 8080) },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.apps.github.com", true, ("a68c43fa9e015e45e046c85d502ec5e4b774", 8080) },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-8080.invalid.host.dev", false, default },
            { "invalidWorkspace-8080.app.vso.io", false, default },
            { "a68c43fa9e015e45e046c85d502ec5e4b774-abc.app.vso.io", false, default },
        };
        
        [Theory]
        [MemberData(nameof(TestHosts))]
        public void PortForwardingHostUtils_IsValidHost(string host, bool isValid)
        {
            var utils = new PortForwardingHostUtils(settings);
            Assert.Equal(isValid, utils.IsPortForwardingHost(host));
        }
        
        [Theory]
        [MemberData(nameof(TestParsedHosts))]
        public void PortForwardingHostUtils_TryGetPortForwardingSessionDetails(string host, bool isValid, (string, int) expectedResult)
        {
            var utils = new PortForwardingHostUtils(settings);
            Assert.Equal(isValid, utils.TryGetPortForwardingSessionDetails(host, out var sessionDetails));
            Assert.Equal(expectedResult, sessionDetails);
        }
    }
}