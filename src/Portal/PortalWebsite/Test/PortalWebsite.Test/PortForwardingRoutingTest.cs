using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VsCloudKernel.Services.Portal.WebSite;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common;
using Moq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortalWebsite.Test
{
    [Trait("Category", "IntegrationTest")]
    public class PortForwardingRoutingTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private WebApplicationFactory<Startup> Factory { get; }

        public PortForwardingRoutingTest(WebApplicationFactory<Startup> factory)
        {
            Factory = factory.WithWebHostBuilder(builder =>
            {
                builder
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        // Setup AppSettings configuration
                        config.AddJsonFile("appsettings.Test.json");
                    })
                    .ConfigureServices((services) =>
                    {
                        var mockKeyVaultSecretReader = new Mock<IKeyVaultSecretReader>();
                        mockKeyVaultSecretReader
                            .Setup(o => o.GetSecretVersionsAsync(It.IsAny<string>(), It.IsAny<string>(),
                                It.IsAny<IDiagnosticsLogger>()))
                            .ReturnsAsync((string vaultName, string secretName, IDiagnosticsLogger logger) => new[]
                                {
                                    new KeyVaultSecret(
                                        id:
                                        $"https://{vaultName}.vault.azure.net/secrets/{secretName}/27a35d5d3731497bbbf64eede9eaf3d3",
                                        attributes: new KeyVaultSecretAttributes(new DateTime(), new DateTime()))
                                });
                        services.Replace(ServiceDescriptor.Singleton(mockKeyVaultSecretReader.Object));
                    });
            });
        }

        public static TheoryData<string> UnhandledPaths = new TheoryData<string>
        {
            "/",
            "/environments",
            "/environment/123",
            "/login",
            "/path.with.dot"
        };

        public static TheoryData<string, string> StaticAssets = new TheoryData<string, string>
        {
            {"/favicon.ico", "image/x-icon"},
            {"/site.css", "text/css"},
            {"/spinner-dark.svg", "image/svg+xml"},
            {"/ms-logo.svg", "image/svg+xml"},
        };

        [Theory]
        [MemberData(nameof(UnhandledPaths))]
        public async Task Portal_UnhandledPath_RespondsWithSPA(string url)
        {
            var client = Factory.CreateClient();

            var message = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await client.SendAsync(message);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseHtml = await response.Content.ReadAsStringAsync();

            Assert.Contains("<title>Visual Studio Codespaces</title>", responseHtml);
        }

        [Theory]
        [MemberData(nameof(UnhandledPaths))]
        public async Task PortForwarding_UnhandledPath_RespondsWithPortForwardingSite(string url)
        {
            var client = Factory.CreateClient();

            var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.Add(PortForwardingHeaders.Token, "super_secret_token");
            message.Headers.Add(PortForwardingHeaders.WorkspaceId, "a68c43fa9e015e45e046c85d502ec5e4b774");
            message.Headers.Add(PortForwardingHeaders.Port, "8080");

            var response = await client.SendAsync(message);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseHtml = await response.Content.ReadAsStringAsync();

            Assert.Contains("Connecting to the forwarded port...", responseHtml);
        }


        [Theory]
        [MemberData(nameof(UnhandledPaths))]
        public async Task PortForwarding_Environment_PassesEnvrironmentIdToView(string url)
        {
            var client = Factory.CreateClient();

            var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.Add(PortForwardingHeaders.Token, "super_secret_token");
            message.Headers.Add(PortForwardingHeaders.WorkspaceId, "a68c43fa9e015e45e046c85d502ec5e4b774");
            message.Headers.Add(PortForwardingHeaders.EnvironmentId, "0b125897-9e5c-438f-b286-a795f0419c3b");
            message.Headers.Add(PortForwardingHeaders.Port, "8080");

            var response = await client.SendAsync(message);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseHtml = await response.Content.ReadAsStringAsync();

            Assert.Contains("environmentId: '0b125897-9e5c-438f-b286-a795f0419c3b'", responseHtml);
        }

        [Fact]
        public async Task PortForwarding_Authentication_RespondsWith401()
        {
            var client = Factory.CreateClient();

            var message = new HttpRequestMessage(HttpMethod.Get, "/auth");
            message.Headers.Add(PortForwardingHeaders.WorkspaceId, "a68c43fa9e015e45e046c85d502ec5e4b774");
            message.Headers.Add(PortForwardingHeaders.Port, "8080");

            var response = await client.SendAsync(message);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Nginx_Authentication_RespondsWith401()
        {
            var client = Factory.CreateClient();

            var message = new HttpRequestMessage(HttpMethod.Get, "/auth");
            message.Headers.Add(PortForwardingHeaders.OriginalUrl, "https://a68c43fa9e015e45e046c85d502ec5e4b774.app.online.dev.core.vsengsaas.visualstudio.com/");

            var response = await client.SendAsync(message);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Portal_Authentication_RespondsWith401()
        {
            var client = Factory.CreateClient();

            var message = new HttpRequestMessage(HttpMethod.Get, "/auth");

            var response = await client.SendAsync(message);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Portal_AuthenticatePortForwarder_RespondsWith200()
        {
            var client = Factory.CreateClient();

            var message = new HttpRequestMessage(HttpMethod.Post, "/authenticate-port-forwarder")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["cascadeToken"] = "abc" })
            };

            var response = await client.SendAsync(message);
            var cookies = response.Headers.GetValues("Set-Cookie");

            Assert.Contains(cookies,
                (cookie) => cookie.StartsWith(Constants.PFCookieName, StringComparison.InvariantCultureIgnoreCase));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PortForwarding_AuthenticatePortForwarder_RespondsWith200()
        {
            var client = Factory.CreateClient();

            var message = new HttpRequestMessage(HttpMethod.Post, "/authenticate-port-forwarder")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["cascadeToken"] = "abc" })
            };
            message.Headers.Add(PortForwardingHeaders.WorkspaceId, "a68c43fa9e015e45e046c85d502ec5e4b774");
            message.Headers.Add(PortForwardingHeaders.Port, "8080");

            var response = await client.SendAsync(message);
            await response.ThrowIfFailedAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var cookies = response.Headers.GetValues("Set-Cookie");
            Assert.Contains(cookies,
                (cookie) => cookie.StartsWith(Constants.PFCookieName, StringComparison.InvariantCultureIgnoreCase));
        }

        [Fact]
        public async Task Portal_LogOutPortForwarder_RespondsWith200()
        {
            var client = Factory.CreateClient();

            var message = new HttpRequestMessage(HttpMethod.Post, "/logout-port-forwarder");

            var response = await client.SendAsync(message);
            await response.ThrowIfFailedAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var cookies = response.Headers.GetValues("Set-Cookie");

            Assert.Contains(cookies,
                (cookie) => cookie.StartsWith(Constants.PFCookieName, StringComparison.InvariantCultureIgnoreCase));
        }

        [Fact]
        public async Task PortForwarding_LogOutPortForwarder_RespondsWith200()
        {
            var client = Factory.CreateClient();

            var message = new HttpRequestMessage(HttpMethod.Post, "/logout-port-forwarder");
            message.Headers.Add(PortForwardingHeaders.WorkspaceId, "a68c43fa9e015e45e046c85d502ec5e4b774");
            message.Headers.Add(PortForwardingHeaders.Port, "8080");

            var response = await client.SendAsync(message);
            await response.ThrowIfFailedAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var cookies = response.Headers.GetValues("Set-Cookie");

            Assert.Contains(cookies,
                (cookie) => cookie.StartsWith(Constants.PFCookieName, StringComparison.InvariantCultureIgnoreCase));
        }

        [Theory]
        [MemberData(nameof(StaticAssets))]
        public async Task Portal_StaticAssets_RespondWithRightMediaType(string asset, string contentType)
        {
            var client = Factory.CreateClient();

            var message = new HttpRequestMessage(HttpMethod.Get, asset);

            var response = await client.SendAsync(message);
            response.EnsureSuccessStatusCode();

            Assert.Equal(contentType,
                response.Content.Headers.ContentType.ToString());
        }

        [Theory]
        [MemberData(nameof(StaticAssets))]
        public async Task PortForwarding_StaticAssets_RespondWithRightMediaType(string asset, string contentType)
        {
            var client = Factory.CreateClient();

            var message = new HttpRequestMessage(HttpMethod.Get, asset);
            message.Headers.Add(PortForwardingHeaders.WorkspaceId, "a68c43fa9e015e45e046c85d502ec5e4b774");
            message.Headers.Add(PortForwardingHeaders.Port, "8080");

            var response = await client.SendAsync(message);
            response.EnsureSuccessStatusCode();

            Assert.Equal(contentType,
                response.Content.Headers.ContentType.ToString());
        }
    }
}