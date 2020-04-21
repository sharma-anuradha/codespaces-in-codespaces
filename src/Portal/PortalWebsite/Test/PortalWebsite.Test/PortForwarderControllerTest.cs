﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.VsCloudKernel.Services.Portal.WebSite;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using Microsoft.VsSaaS.Diagnostics;
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
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(await controller.AuthAsync(workspaceInfo, logger));

            Assert.Equal(401, result.StatusCode);
        }

        [Fact]
        public async Task AuthAsync_WithAuthHeader_200()
        {
            var headers = new Dictionary<string, string>
            {
                [PortForwardingHeaders.OriginalUrl] = "https://a68c43fa9e015e45e046c85d502ec5e4b774-8080.apps.github.com/stuff",
            };

            var controller = CreateController(headers);
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(await controller.AuthAsync(workspaceInfo, logger));

            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task AuthAsync_WithCookie_200()
        {
            var headers = new Dictionary<string, string>
            {
                [PortForwardingHeaders.OriginalUrl] = "https://a68c43fa9e015e45e046c85d502ec5e4b774-8080.apps.github.com/stuff",
            };

            var controller = CreateController(headers, useAuthCookie: true);
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(await controller.AuthAsync(workspaceInfo, logger));

            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task AuthAsync_WithEnvironmentSpecificCookie_200()
        {
            var headers = new Dictionary<string, string>
            {
                [PortForwardingHeaders.OriginalUrl] = "https://92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.github.com/stuff",
            };

            var controller = CreateController(
                headers,
                useAuthCookie: true,
                environmentId: "92617f60-2f2c-4986-85a0-ce95ceb3a658",
                connectionSessionId: "a68c43fa9e015e45e046c85d502ec5e4b774");
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(await controller.AuthAsync(workspaceInfo, logger));

            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public async Task AuthAsync_WithEnvironmentSpecificCookie_NotMatchingEnvId_200()
        {
            var headers = new Dictionary<string, string>
            {
                [PortForwardingHeaders.OriginalUrl] = "https://d8b8515e-0f5d-4766-8310-4846c61990a5-8080.apps.github.com/stuff",
            };

            var controller = CreateController(
                headers,
                useAuthCookie: true,
                environmentId: "92617f60-2f2c-4986-85a0-ce95ceb3a658",
                connectionSessionId: "a68c43fa9e015e45e046c85d502ec5e4b774");
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(await controller.AuthAsync(workspaceInfo, logger));

            Assert.Equal(401, result.StatusCode);
        }


        [Fact]
        public async Task AuthAsync_WithEnvironmentSpecificCookie_SetsHeaders()
        {
            var headers = new Dictionary<string, string>
            {
                [PortForwardingHeaders.OriginalUrl] = "https://92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.github.com/stuff",
            };

            var controller = CreateController(
                headers,
                useAuthCookie: true,
                environmentId: "92617f60-2f2c-4986-85a0-ce95ceb3a658",
                connectionSessionId: "A68C43FA9E015E45E046C85D502EC5E4B774");
            var result = Assert.IsAssignableFrom<IStatusCodeActionResult>(await controller.AuthAsync(workspaceInfo, logger));

            Assert.Equal(200, result.StatusCode);

            Assert.Equal("A68C43FA9E015E45E046C85D502EC5E4B774", controller.HttpContext.Response.Headers[PortForwardingHeaders.WorkspaceId].ToString());
            Assert.Equal("92617f60-2f2c-4986-85a0-ce95ceb3a658", controller.HttpContext.Response.Headers[PortForwardingHeaders.EnvironmentId].ToString());
            Assert.Equal("8080", controller.HttpContext.Response.Headers[PortForwardingHeaders.Port].ToString());
        }

        [Fact]
        public void SignIn_GitHub_RedirectsWithEnvironmentId()
        {
            var controller = CreateController(host: "92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.github.com");

            var returnUrl = new Uri("https://92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.github.com/");
            var result = Assert.IsAssignableFrom<RedirectResult>(controller.SignIn(returnUrl));
            var redirectUri = new Uri(result.Url);

            Assert.Equal("github.com", redirectUri.Host);
            Assert.Equal("/workspaces/auth/92617f60-2f2c-4986-85a0-ce95ceb3a658", redirectUri.AbsolutePath);

            var queryString = HttpUtility.ParseQueryString(redirectUri.Query);

            Assert.Equal("8080", queryString.Get("port"));
            Assert.Null(queryString.Get("path"));
            Assert.Null(queryString.Get("query"));
        }

        [Fact]
        public void SignIn_GitHub_RedirectsWithPath()
        {
            var controller = CreateController(host: "92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.github.com");

            var returnUrl = new Uri("https://92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.github.com/stuff");
            var result = Assert.IsAssignableFrom<RedirectResult>(controller.SignIn(returnUrl));
            var redirectUri = new Uri(result.Url);

            Assert.Equal("github.com", redirectUri.Host);
            Assert.Equal("/workspaces/auth/92617f60-2f2c-4986-85a0-ce95ceb3a658", redirectUri.AbsolutePath);

            var queryString = HttpUtility.ParseQueryString(redirectUri.Query);

            Assert.Equal("8080", queryString.Get("port"));
            Assert.Equal("/stuff", queryString.Get("path"));
            Assert.Null(queryString.Get("query"));
        }

        [Fact]
        public void SignIn_VSOnline_RedirectsWithPathAndQuery()
        {
            var controller = CreateController(host: "92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.app.online.visualstudio.com");

            var returnUrl = new Uri("https://92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.github.com/stuff?search=42");
            var result = Assert.IsAssignableFrom<RedirectResult>(controller.SignIn(returnUrl));
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
            var controller = CreateController(host: "92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.github.com");

            var returnUrl = new Uri("https://92617f60-2f2c-4986-85a0-ce95ceb3a658-8080.apps.github.com/stuff?search=42");
            var result = Assert.IsAssignableFrom<RedirectResult>(controller.SignIn(returnUrl));
            var redirectUri = new Uri(result.Url);

            Assert.Equal("github.com", redirectUri.Host);
            Assert.Equal("/workspaces/auth/92617f60-2f2c-4986-85a0-ce95ceb3a658", redirectUri.AbsolutePath);

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

            var controller = new PortForwarderController(MockAppSettings.Settings, hostUtils, cookieEncryptionProvider, hostEnvironment)
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
