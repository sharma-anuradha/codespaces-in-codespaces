using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.VsCloudKernel.Services.Portal.WebSite;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Clients;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Models;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Moq;
using System.Threading.Tasks;
using System.Web;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortalWebsite.Test
{
    public class AuthControllerTest
    {
        private readonly IDiagnosticsLogger logger;
        private readonly CookieEncryptionUtils cookieEncryptionUtils;
        private MockHttpContext currentContext;

        public AuthControllerTest()
        {
            IDiagnosticsLoggerFactory loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();

            cookieEncryptionUtils = new CookieEncryptionUtils(MockAppSettings.Settings);
        }

        [Fact]
        public async Task AuthenticatePortForwarderAsync_BadRequest()
        {
            var controller = CreateController();
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(await controller.AuthenticatePortForwarderAsync(null, null));

            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task AuthenticatePortForwarderAsync_WithCascadeToken_200()
        {
            var controller = CreateController();
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(
                await controller.AuthenticatePortForwarderAsync(null, cascadeToken: "super_secret_token"));

            Assert.Equal(200, result.StatusCode);

            currentContext.MockResponseCookies.Verify(
                cookies => cookies.Append(Constants.PFCookieName, It.IsAny<string>(), It.IsAny<CookieOptions>()));
        }

        public static TheoryData<string, string, string, int?> BadAuthRequests = new TheoryData<string, string, string, int?>
        {
            { null, null, "50994a51-61f4-4f4c-b31d-f58bbef21fd2", 1234},
            { "aadToken", "cascadeToken", "50994a51-61f4-4f4c-b31d-f58bbef21fd2", 1234},
            { "aadToken", null, null, 1234},
            { "aadToken", null, "50994a51-61f4-4f4c-b31d-f58bbef21fd2", 0},
            { "aadToken", null, "50994a51-61f4-4f4c-b31d-f58bbef21fd2", 65536},
        };

        [Theory]
        [MemberData(nameof(BadAuthRequests))]
        public async Task AuthenticateEnvironmentAsync_InvalidRequests_400(string token, string cascadeToken, string environmentId, int port)
        {
            var controller = CreateController();
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(
                await controller.AuthenticateCodespaceAsync(
                    environmentId,
                    token,
                    cascadeToken,
                    port,
                    path: null,
                    query: null,
                    logger: logger));

            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public async Task AuthenticateEnvironmentAsync_NoEnvironment_404()
        {
            var client = new Mock<IFrontEndWebApiClient>();
            client.Setup(c => c.GetEnvironmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                  .ReturnsAsync(null as CloudEnvironmentResult);

            var controller = CreateController(client.Object);
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(
                await controller.AuthenticateCodespaceAsync(
                    "environmentId",
                    "aadToken",
                    cascadeToken: null,
                    port: 1234,
                    path: null,
                    query: null,
                    logger: logger));

            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task AuthenticateEnvironmentAsync_NoConnectionSessionId_503()
        {
            var client = new Mock<IFrontEndWebApiClient>();
            client.Setup(c => c.GetEnvironmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                  .ReturnsAsync(new CloudEnvironmentResult());

            var controller = CreateController(client.Object);
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(
                await controller.AuthenticateCodespaceAsync(
                    "environmentId",
                    "aadToken",
                    cascadeToken: null,
                    port: 1234,
                    path: null,
                    query: null,
                    logger: logger));

            Assert.Equal(503, result.StatusCode);
        }

        [Fact]
        public async Task AuthenticateEnvironmentAsync_CannotExchangeToken_403()
        {
            var client = new Mock<IFrontEndWebApiClient>();
            var environment = new CloudEnvironmentResult
            {
                Connection = new ConnectionInfoBody
                {
                    ConnectionSessionId = "ABCDEF0123456789"
                }
            };
            client.Setup(c => c.GetEnvironmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                  .ReturnsAsync(environment);

            var controller = CreateController(client.Object);
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(
                await controller.AuthenticateCodespaceAsync(
                    "environmentId",
                    token: "aadToken",
                    cascadeToken: null,
                    port: 1234,
                    path: null,
                    query: null,
                    logger: logger));

            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task AuthenticateEnvironmentAsync_ConnectionSessionId_302()
        {
            var client = new Mock<IFrontEndWebApiClient>();
            var environment = new CloudEnvironmentResult
            {
                Connection = new ConnectionInfoBody
                {
                    ConnectionSessionId = "ABCDEF0123456789"
                }
            };
            client.Setup(c => c.GetEnvironmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                  .ReturnsAsync(environment);

            var controller = CreateController(client.Object);
            var result = Assert.IsAssignableFrom<RedirectResult>(
                await controller.AuthenticateCodespaceAsync(
                    "environmentId",
                    token: null,
                    cascadeToken: "cascadeToken",
                    port: 1234,
                    path: null,
                    query: null,
                    logger: logger));

            Assert.Equal("https://environmentid-1234.app.online.visualstudio.com/", result.Url);
        }

        [Fact]
        public async Task AuthenticateEnvironmentAsync_ConnectionSessionId_SetsPath()
        {
            var client = new Mock<IFrontEndWebApiClient>();
            var environment = new CloudEnvironmentResult
            {
                Connection = new ConnectionInfoBody
                {
                    ConnectionSessionId = "ABCDEF0123456789"
                }
            };
            client.Setup(c => c.GetEnvironmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                  .ReturnsAsync(environment);

            var controller = CreateController(client.Object);
            var result = Assert.IsAssignableFrom<RedirectResult>(
                await controller.AuthenticateCodespaceAsync(
                    "environmentId",
                    token: null,
                    cascadeToken: "cascadeToken",
                    port: 1234,
                    path: "/some/deep/path/with.file.ext",
                    query: null,
                    logger: logger));

            Assert.Equal("https://environmentid-1234.app.online.visualstudio.com/some/deep/path/with.file.ext", result.Url);
        }

        [Fact]
        public async Task AuthenticateEnvironmentAsync_ConnectionSessionId_KeepsQuery()
        {
            var client = new Mock<IFrontEndWebApiClient>();
            var environment = new CloudEnvironmentResult
            {
                Connection = new ConnectionInfoBody
                {
                    ConnectionSessionId = "ABCDEF0123456789"
                }
            };
            client.Setup(c => c.GetEnvironmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                  .ReturnsAsync(environment);

            var controller = CreateController(client.Object);
            var result = Assert.IsAssignableFrom<RedirectResult>(
                await controller.AuthenticateCodespaceAsync(
                    "environmentId",
                    token: null,
                    cascadeToken: "cascadeToken",
                    port: 1234,
                    path: "/some",
                    query: "and=query",
                    logger: logger));

            Assert.Equal("https://environmentid-1234.app.online.visualstudio.com/some?and=query", result.Url);
        }

        [Fact]
        public async Task AuthenticateEnvironmentAsync_ConnectionSessionId_CookieHasConnectionSessionId()
        {
            var client = new Mock<IFrontEndWebApiClient>();
            var environment = new CloudEnvironmentResult
            {
                Connection = new ConnectionInfoBody
                {
                    ConnectionSessionId = "ABCDEF0123456789"
                }
            };
            client.Setup(c => c.GetEnvironmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                  .ReturnsAsync(environment);

            var controller = CreateController(client.Object);
            var result = Assert.IsAssignableFrom<RedirectResult>(
                await controller.AuthenticateCodespaceAsync(
                    "environmentId",
                    token: null,
                    cascadeToken: "cascadeToken",
                    port: 1234,
                    path: "/some",
                    query: "and=query",
                    logger: logger));

            currentContext.MockResponseCookies.Verify(
                c => c.Append(
                    Constants.PFCookieName,
                    It.Is<string>(
                        value => cookieEncryptionUtils.DecryptCookie(value).ConnectionSessionId == environment.Connection.ConnectionSessionId),
                    It.IsAny<CookieOptions>())
            );
        }

        [Fact]
        public async Task AuthenticateEnvironmentAsync_ConnectionSessionId_CookieHasEnvironmentId()
        {
            var client = new Mock<IFrontEndWebApiClient>();
            var environment = new CloudEnvironmentResult
            {
                Connection = new ConnectionInfoBody
                {
                    ConnectionSessionId = "ABCDEF0123456789"
                }
            };
            client.Setup(c => c.GetEnvironmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                  .ReturnsAsync(environment);

            var controller = CreateController(client.Object);
            var result = Assert.IsAssignableFrom<RedirectResult>(
                await controller.AuthenticateCodespaceAsync(
                    "environmentId",
                    token: null,
                    cascadeToken: "cascadeToken",
                    port: 1234,
                    path: "/some",
                    query: "and=query",
                    logger: logger));

            currentContext.MockResponseCookies.Verify(
                c => c.Append(
                    Constants.PFCookieName,
                    It.Is<string>(
                        value => cookieEncryptionUtils.DecryptCookie(value).EnvironmentId == "environmentId"),
                    It.IsAny<CookieOptions>())
            );
        }
        [Fact]
        public void AuthenticateWorkspaceAsync_RendersForm_PassesParameters()
        {
            var controller = CreateController(host: "auth.apps.github.com", query: "?path=abc&port=1234&query=def");
            var result = Assert.IsAssignableFrom<ViewResult>(
                controller.AuthenticateWorkspaceAsync(
                    "environmentId",
                    cascadeToken: "cascadeToken",
                    port: 1234));

            var formDetails = Assert.IsType<AuthenticateWorkspaceFormDetails>(result.Model);

            Assert.Equal("https://environmentid-1234.apps.test.workspaces.githubusercontent.com/authenticate-codespace/environmentId?path=abc&port=1234&query=def", formDetails.Action);
            Assert.Equal("cascadeToken", formDetails.CascadeToken);
        }

        private AuthController CreateController(
            IFrontEndWebApiClient frontEndWebApiClient = null,
            ILiveShareTokenExchangeUtil tokenExchangeUtil = null,
            string host = null,
            string path = null,
            string query = null)
        {
            currentContext = MockHttpContext.Create();

            if (host != null)
            {
                currentContext.Request.Host = HostString.FromUriComponent(host);
            }

            if (path != null)
            {
                currentContext.Request.Path = path;
            }

            if (query != null)
            {
                currentContext.Request.QueryString = new QueryString(query);
            }

            var controller = new AuthController(
                MockAppSettings.Settings,
                cookieEncryptionUtils,
                frontEndWebApiClient ?? new Mock<IFrontEndWebApiClient>().Object,
                tokenExchangeUtil ?? new Mock<ILiveShareTokenExchangeUtil>().Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = currentContext,
                }
            };

            return controller;
        }
    }
}
