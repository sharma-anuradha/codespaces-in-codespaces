using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.TokenService.Authentication;
using Microsoft.VsSaaS.Services.TokenService.Contracts;
using Moq;
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
            var authHandler = CreateAuthenticationHandler(CreateMockHttpClient(
                new GitHubUser
                {
                    id = TestId,
                    login = TestLogin,
                    name = TestName,
                    email = TestEmail,
                },
                authorizeApp: true));
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
            var authHandler = CreateAuthenticationHandler(CreateMockHttpClient(
                new GitHubUser(), authorizeApp: true));
            await authHandler.InitializeAsync(
                MockAuthenticationScheme, mockHttpContext);
            var result = await authHandler.AuthenticateAsync();
            Assert.NotNull(result.Failure);
        }

        [Fact]
        public async Task GithubAuthenticationHeaderNoEmail()
        {
            var mockHttpContext = MockHttpContext(
                new AuthenticationHeaderValue(TestScheme, TestToken));
            var authHandler = CreateAuthenticationHandler(CreateMockHttpClient(
                new GitHubUser { id = TestId, login = TestLogin, name = TestName }, authorizeApp: true));
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
        public async Task GithubAuthenticationHeaderUserValidationFailed()
        {
            var mockHttpContext = MockHttpContext(
                new AuthenticationHeaderValue(TestScheme, TestToken));
            var authHandler = CreateAuthenticationHandler(
                CreateMockHttpClient(authorizeUser: null, authorizeApp: true));
            await authHandler.InitializeAsync(
                MockAuthenticationScheme, mockHttpContext);
            var result = await authHandler.AuthenticateAsync();
            Assert.Equal("Invalid GitHub user token.", result.Failure?.Message);
        }

        [Fact]
        public async Task GithubAuthenticationHeaderAppValidationFailed()
        {
            var mockHttpContext = MockHttpContext(
                new AuthenticationHeaderValue(TestScheme, TestToken));
            var authHandler = CreateAuthenticationHandler(CreateMockHttpClient(
                new GitHubUser { id = TestId, login = TestLogin }, authorizeApp: false));
            await authHandler.InitializeAsync(
                MockAuthenticationScheme, mockHttpContext);
            var result = await authHandler.AuthenticateAsync();
            Assert.Equal("GitHub user token is not authorized for this application.", result.Failure?.Message);
        }

        private static GithubAuthenticationHandler CreateAuthenticationHandler(HttpClient httpClient)
        {
            return new GithubAuthenticationHandler(
                MockOptionsMonitor<AuthenticationSchemeOptions>(),
                MockLoggerFactory(),
                new Mock<UrlEncoder>(MockBehavior.Strict).Object,
                new Mock<ISystemClock>(MockBehavior.Strict).Object,
                MockHttpClientProvider<IGithubApiHttpClientProvider>(httpClient));
        }

        private static HttpClient CreateMockHttpClient(GitHubUser authorizeUser, bool authorizeApp)
        {
            string username = authorizeUser?.login ?? string.Empty;
            return MockHttpClient((request) =>
            {
                var requestPath = request.RequestUri.PathAndQuery;
                if (requestPath.EndsWith("/user"))
                {
                    if (authorizeUser != null)
                    {
                        var json = JsonSerializer.Serialize(authorizeUser);
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(json),
                        };
                    }
                    else
                    {
                        return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    }
                }
                else if (requestPath.EndsWith($"{username}/codespaces"))
                {
                    return new HttpResponseMessage(
                        authorizeApp ? HttpStatusCode.OK : HttpStatusCode.Unauthorized);
                }
                else
                {
                    Assert.True(false, $"Unexpected HTTP request path: {requestPath}");
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            });
        }

        private class GitHubUser
        {
            public int? id { get; set; }
            public string login { get; set; }
            public string name { get; set; }
            public string email { get; set; }
        }
    }
}