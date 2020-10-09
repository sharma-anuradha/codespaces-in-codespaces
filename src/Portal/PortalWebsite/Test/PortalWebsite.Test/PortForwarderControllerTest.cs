using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.VsCloudKernel.Services.Portal.WebSite;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.CodespacesApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortalWebsite.Test
{
    public class PortForwarderControllerTest
    {
        private readonly IDiagnosticsLogger logger;
        private readonly PortForwardingHostUtils hostUtils;
        private readonly IHostEnvironment hostEnvironment;
        private readonly IWorkspaceInfo workspaceInfo;

        public PortForwarderControllerTest()
        {
            IDiagnosticsLoggerFactory loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();

            hostUtils = new PortForwardingHostUtils(MockAppSettings.Settings.PortForwardingHostsConfigs);

            hostEnvironment = new Mock<IHostEnvironment>().Object;

            var workspaceInfoMock = new Mock<IWorkspaceInfo>();
            workspaceInfoMock.Setup(wi => wi.GetWorkSpaceOwner(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("a_b");
            workspaceInfo = workspaceInfoMock.Object;
        }

        [Fact]
        public async Task AuthAsync_401()
        {
            var controller = CreateController(isAuthenticated: false);
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(await controller.AuthAsync(skipFetchingCodespace: false, workspaceInfo, logger));

            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task AuthAsync_WithAuthHeader_200()
        {
            var headers = new Dictionary<string, string>
            {
                [PortForwardingHeaders.OriginalUrl] = "https://a68c43fa9e015e45e046c85d502ec5e4b774-8080.apps.test.codespaces.githubusercontent.com/stuff",
            };

            var controller = CreateController(headers);
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(await controller.AuthAsync(skipFetchingCodespace: false, workspaceInfo, logger));

            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task AuthAsync_WithCookie_200()
        {
            var headers = new Dictionary<string, string>
            {
                [PortForwardingHeaders.OriginalUrl] = "https://a68c43fa9e015e45e046c85d502ec5e4b774-8080.apps.test.codespaces.githubusercontent.com/stuff",
            };

            var controller = CreateController(headers, useAuthCookie: true);
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(await controller.AuthAsync(skipFetchingCodespace: false, workspaceInfo, logger));

            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task AuthAsync_WithEnvironmentSpecificCookie_200()
        {
            var headers = new Dictionary<string, string>
            {
                [PortForwardingHeaders.OriginalUrl] = "https://92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.test.codespaces.githubusercontent.com/stuff",
            };

            var controller = CreateController(
                headers,
                useAuthCookie: true,
                environmentId: "92617f60-2f2c-4986-85a0-ce95ceb3a658",
                connectionSessionId: "a68c43fa9e015e45e046c85d502ec5e4b774");
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(await controller.AuthAsync(skipFetchingCodespace: false, workspaceInfo, logger));

            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task AuthAsync_WithEnvironmentSpecificCookie_NotMatchingEnvId_200()
        {
            var headers = new Dictionary<string, string>
            {
                [PortForwardingHeaders.OriginalUrl] = "https://d8b8515e-0f5d-4766-8310-4846c61990a5-8080.apps.test.codespaces.githubusercontent.com/stuff",
            };

            var controller = CreateController(
                headers,
                useAuthCookie: true,
                environmentId: "92617f60-2f2c-4986-85a0-ce95ceb3a658",
                connectionSessionId: "a68c43fa9e015e45e046c85d502ec5e4b774");
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(await controller.AuthAsync(skipFetchingCodespace: false, workspaceInfo, logger));

            Assert.Equal(401, result.StatusCode);
        }


        [Fact]
        public async Task AuthAsync_WithEnvironmentSpecificCookie_SetsHeaders()
        {
            var headers = new Dictionary<string, string>
            {
                [PortForwardingHeaders.OriginalUrl] = "https://92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.test.codespaces.githubusercontent.com/stuff",
            };

            var controller = CreateController(
                headers,
                useAuthCookie: true,
                environmentId: "92617f60-2f2c-4986-85a0-ce95ceb3a658",
                connectionSessionId: "A68C43FA9E015E45E046C85D502EC5E4B774");
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(await controller.AuthAsync(skipFetchingCodespace: false, workspaceInfo, logger));

            Assert.Equal(200, result.StatusCode);

            Assert.Equal("A68C43FA9E015E45E046C85D502EC5E4B774", controller.HttpContext.Response.Headers[PortForwardingHeaders.WorkspaceId].ToString());
            Assert.Equal("92617f60-2f2c-4986-85a0-ce95ceb3a658", controller.HttpContext.Response.Headers[PortForwardingHeaders.EnvironmentId].ToString());
            Assert.Equal("8080", controller.HttpContext.Response.Headers[PortForwardingHeaders.Port].ToString());
        }

        [Fact]
        public void SignIn_VSO_WorkspaceIdServiceWorkerResponse()
        {
            var controller = CreateController(host: "portal-vsclk-portal-website.default.svc.cluster.local");

            var returnUrl = new Uri("https://4a048223cc254c1aeeba1c1e8078a6b3d761-8080.app.online.visualstudio.com/");
            var result = Assert.IsAssignableFrom<ViewResult>(controller.SignIn(returnUrl, logger));

            Assert.Equal("exception", result.ViewName);
        }

        [Fact]
        public void SignIn_Github_WorkspaceIdServiceWorkerResponse()
        {
            var controller = CreateController(host: "portal-vsclk-portal-website.default.svc.cluster.local");

            var returnUrl = new Uri("https://4a048223cc254c1aeeba1c1e8078a6b3d761-8080.apps.test.codespaces.githubusercontent.com/");
            var result = Assert.IsAssignableFrom<RedirectResult>(controller.SignIn(returnUrl, logger));

            Assert.Equal("https://github.com/404", result.Url);
        }

        [Fact]
        public void SignIn_GitHub_RedirectsWithEnvironmentId()
        {
            var controller = CreateController(host: "portal-vsclk-portal-website.default.svc.cluster.local");

            var returnUrl = new Uri("https://92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.test.codespaces.githubusercontent.com/");
            var result = Assert.IsAssignableFrom<RedirectResult>(controller.SignIn(returnUrl, logger));
            var redirectUri = new Uri(result.Url);

            Assert.Equal("github.com", redirectUri.Host);
            Assert.Equal("/codespaces/auth/92617f60-2f2c-4986-85a0-ce95ceb3a658", redirectUri.AbsolutePath);

            var queryString = HttpUtility.ParseQueryString(redirectUri.Query);

            Assert.Equal("8080", queryString.Get("port"));
            Assert.Null(queryString.Get("path"));
            Assert.Null(queryString.Get("query"));
        }

        [Fact]
        public void SignIn_GitHub_RedirectsWithPath()
        {
            var controller = CreateController(host: "portal-vsclk-portal-website.default.svc.cluster.local");

            var returnUrl = new Uri("https://92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.test.codespaces.githubusercontent.com/stuff");
            var result = Assert.IsAssignableFrom<RedirectResult>(controller.SignIn(returnUrl, logger));
            var redirectUri = new Uri(result.Url);

            Assert.Equal("github.com", redirectUri.Host);
            Assert.Equal("/codespaces/auth/92617f60-2f2c-4986-85a0-ce95ceb3a658", redirectUri.AbsolutePath);

            var queryString = HttpUtility.ParseQueryString(redirectUri.Query);

            Assert.Equal("8080", queryString.Get("port"));
            Assert.Equal("/stuff", queryString.Get("path"));
            Assert.Null(queryString.Get("query"));
        }

        [Fact]
        public void SignIn_VSOnline_RedirectsWithPathAndQuery()
        {
            var controller = CreateController(host: "portal-vsclk-portal-website.default.svc.cluster.local");

            var returnUrl = new Uri("https://92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.app.online.visualstudio.com/stuff?search=42");
            var result = Assert.IsAssignableFrom<RedirectResult>(controller.SignIn(returnUrl, logger));
            var redirectUri = new Uri(result.Url);

            Assert.Equal("fake.portal.dev", redirectUri.Host);
            Assert.Equal("/port-forwarding-sign-in/92617f60-2f2c-4986-85a0-ce95ceb3a658", redirectUri.AbsolutePath);

            var queryString = HttpUtility.ParseQueryString(redirectUri.Query);

            Assert.Equal("8080", queryString.Get("port"));
            Assert.Equal("/stuff", queryString.Get("path"));
            Assert.Equal("?search=42", queryString.Get("query"));
        }

        [Fact]
        public void SignIn_GitHub_RedirectsWithPathAndQuery()
        {
            var controller = CreateController(host: "portal-vsclk-portal-website.default.svc.cluster.local");

            var returnUrl = new Uri("https://92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.test.codespaces.githubusercontent.com/stuff?search=42");
            var result = Assert.IsAssignableFrom<RedirectResult>(controller.SignIn(returnUrl, logger));
            var redirectUri = new Uri(result.Url);

            Assert.Equal("github.com", redirectUri.Host);
            Assert.Equal("/codespaces/auth/92617f60-2f2c-4986-85a0-ce95ceb3a658", redirectUri.AbsolutePath);

            var queryString = HttpUtility.ParseQueryString(redirectUri.Query);

            Assert.Equal("8080", queryString.Get("port"));
            Assert.Equal("/stuff", queryString.Get("path"));
            Assert.Equal("?search=42", queryString.Get("query"));
        }

        private PortForwarderController CreateController(
            IDictionary<string, string> headers = default,
            bool isAuthenticated = true,
            bool useAuthCookie = false,
            string environmentId = null,
            string connectionSessionId = null,
            string host = null)
        {
            var httpContext = MockHttpContext.Create();
            var cookieEncryptionProvider = new CookieEncryptionUtils(MockAppSettings.Settings);

            if (host != null)
            {
                httpContext.Request.Host = HostString.FromUriComponent(host);
            }

            if (headers == default)
            {
                headers = new Dictionary<string, string>();
            }

            if (isAuthenticated)
            {
                var token = TestTokenWriter.WriteToken(
                    new JwtPayload(new[]
                    {
                        new Claim("tid", "a"),
                        new Claim("oid", "b"),
                    }),
                    logger);
                if (useAuthCookie)
                {
                    var cookieCollection = new Mock<IRequestCookieCollection>();
                    cookieCollection.SetupGet(
                        p => p[Constants.PFCookieName]).Returns(cookieEncryptionProvider.GetEncryptedCookieContent(
                            token,
                            environmentId,
                            connectionSessionId));
                    httpContext.Request.Cookies = cookieCollection.Object;
                }
                else
                {
                    headers.Add(PortForwardingHeaders.Authentication, token);
                }
            }

            foreach (var header in headers)
            {
                httpContext.Request.Headers.Add(header.Key, header.Value);
            }

            var codespacesApiClient = new Mock<ICodespacesApiClient>();

            var controller = new PortForwarderController(MockAppSettings.Settings, hostUtils, cookieEncryptionProvider, codespacesApiClient.Object, hostEnvironment)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext,
                }
            };

            return controller;
        }
    }
}
