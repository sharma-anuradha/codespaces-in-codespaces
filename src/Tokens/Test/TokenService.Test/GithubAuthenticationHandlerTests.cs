using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.TokenService.Authentication;
using Microsoft.VsSaaS.Services.TokenService.Contracts;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.VsSaaS.Services.TokenService.Test
{
    using static MockUtil;

    public class GithubAuthenticationHandlerTests
    {
        private const string TestScheme = "test";
        private const string TestToken = "000";
        private const int TestId = 123;
        private const string TestLogin = "user";
        private const string TestName = "Test User";
        private const string TestEmail = "user@example.com";
        private static readonly AuthenticationScheme MockAuthenticationScheme =
            new AuthenticationScheme(TestScheme, "Test", typeof(GithubAuthenticationHandler));

        [Fact]
        public async Task NoAuthenticationHeader()
        {
            var mockHttpContext = MockHttpContext(null);
            var authHandler = CreateAuthenticationHandler(null);
            await authHandler.InitializeAsync(
                MockAuthenticationScheme, mockHttpContext);
            var result = await authHandler.AuthenticateAsync();
            Assert.True(result.None);
        }

        [Fact]
        public async Task OtherAuthenticationHeader()
        {
            var mockHttpContext = MockHttpContext(
                new AuthenticationHeaderValue("other", TestToken));
            var authHandler = CreateAuthenticationHandler(null);
            await authHandler.InitializeAsync(
                MockAuthenticationScheme, mockHttpContext);
            var result = await authHandler.AuthenticateAsync();
            Assert.True(result.None);
        }

        [Fact]
        public async Task GithubAuthenticationHeader()
        {
            var mockHttpContext = MockHttpContext(
                new AuthenticationHeaderValue(TestScheme, TestToken));
            var authHandler = CreateAuthenticationHandler(MockHttpClient(new
            {
                id = TestId,
                name = TestName,
                login = TestLogin,
                email = TestEmail,
            }));
            await authHandler.InitializeAsync(
                MockAuthenticationScheme, mockHttpContext);
            var result = await authHandler.AuthenticateAsync();
            Assert.Null(result.Failure);
            Assert.True(result.Succeeded);

            var identity = (ClaimsIdentity)result.Ticket.Principal.Identity;
            Assert.True(identity.IsAuthenticated);
            Assert.Equal(TestName, identity.Name);
            Assert.Equal(ProviderNames.GitHub, identity.FindFirst(CustomClaims.TenantId)?.Value);
            Assert.Equal(TestId.ToString(), identity.FindFirst(CustomClaims.OId)?.Value);
            Assert.Equal(TestLogin, identity.FindFirst(CustomClaims.Username)?.Value);
            Assert.Equal(TestEmail, identity.FindFirst(CustomClaims.Email)?.Value);
        }

        [Fact]
        public async Task GithubAuthenticationHeaderNoId()
        {
            var mockHttpContext = MockHttpContext(
                new AuthenticationHeaderValue(TestScheme, TestToken));
            var authHandler = CreateAuthenticationHandler(MockHttpClient(new
            {
                name = TestName,
                login = TestLogin,
            }));
            await authHandler.InitializeAsync(
                MockAuthenticationScheme, mockHttpContext);
            var result = await authHandler.AuthenticateAsync();
            Assert.NotNull(result.Failure);
        }

        [Fact]
        public async Task GithubAuthenticationHeaderNoEmail()
        {
            var mockHttpContext = MockUtil.MockHttpContext(
                new AuthenticationHeaderValue(TestScheme, TestToken));
            var authHandler = CreateAuthenticationHandler(MockUtil.MockHttpClient(new
            {
                id = TestId,
                name = TestName,
                login = TestLogin,
            }));
            await authHandler.InitializeAsync(
                MockAuthenticationScheme, mockHttpContext);
            var result = await authHandler.AuthenticateAsync();
            Assert.Null(result.Failure);
            Assert.True(result.Succeeded);

            var identity = (ClaimsIdentity)result.Ticket.Principal.Identity;
            Assert.True(identity.IsAuthenticated);
            Assert.Equal(TestName, identity.Name);
            Assert.Equal(ProviderNames.GitHub, identity.FindFirst(CustomClaims.TenantId)?.Value);
            Assert.Equal(TestId.ToString(), identity.FindFirst(CustomClaims.OId)?.Value);
            Assert.Equal(TestLogin, identity.FindFirst(CustomClaims.Username)?.Value);
            Assert.False(string.IsNullOrEmpty(identity.FindFirst(CustomClaims.Email)?.Value));
        }

        [Fact]
        public async Task GithubAuthenticationHeaderValidationFailed()
        {
            var mockHttpContext = MockHttpContext(
                new AuthenticationHeaderValue(TestScheme, TestToken));
            var authHandler = CreateAuthenticationHandler(MockHttpClient(
                (request) => new HttpResponseMessage(HttpStatusCode.Unauthorized)));
            await authHandler.InitializeAsync(
                MockAuthenticationScheme, mockHttpContext);
            var result = await authHandler.AuthenticateAsync();
            Assert.NotNull(result.Failure);
        }

        private static GithubAuthenticationHandler CreateAuthenticationHandler(HttpClient httpClient)
        {
            return new GithubAuthenticationHandler(
                MockOptionsMonitor<AuthenticationSchemeOptions>(),
                MockLoggerFactory(),
                new Mock<UrlEncoder>(MockBehavior.Strict).Object,
                new Mock<ISystemClock>(MockBehavior.Strict).Object,
                MockHttpClientProvider<IGithubApiHttpClientProvider>(httpClient),
                new DefaultLoggerFactory());
        }
    }
}