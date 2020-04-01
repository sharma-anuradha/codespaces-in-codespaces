using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.TokenService.Client;
using Microsoft.VsSaaS.Services.TokenService.Contracts;
using Xunit;

namespace Microsoft.VsSaaS.Services.TokenService.Test
{
    using static TokenServiceFixture;

    public class TokenServiceExchangeTests : TokenServiceTestsBase
    {
        public TokenServiceExchangeTests(TokenServiceFixture tokenService) : base(tokenService)
        {
        }

        [Fact]
        public async Task ExchangeTokenWithDefaults()
        {
            var expectedExpiration = DateTime.UtcNow + ExchangeLifetime;

            var payload = await ExchangeTokenAsync(null, null);

            Assert.Equal(TestAudience1, payload.Aud?.SingleOrDefault());

            var exp = payload.ValidTo;
            AssertTime(expectedExpiration, exp);
        }

        [Fact]
        public async Task ExchangeTokenWithSpecifiedLifetime()
        {
            var testLifetime = TimeSpan.FromMinutes(30);
            var expectedExpiration = DateTime.UtcNow + testLifetime;

            var payload = await ExchangeTokenAsync(null, testLifetime);
            var exp = payload.ValidTo;
            AssertTime(expectedExpiration, exp);
        }

        [Fact]
        public async Task ExchangeTokenWithMaximumLifetime()
        {
            var testLifetime = TimeSpan.FromDays(100);
            var expectedExpiration = DateTime.UtcNow + ExchangeLifetime;

            var payload = await ExchangeTokenAsync(null, testLifetime);
            var exp = payload.ValidTo;
            AssertTime(expectedExpiration, exp);
        }

        [Fact]
        public async Task ExchangeTokenWithSpecifiedAudience()
        {
            var payload = await ExchangeTokenAsync(TestAudience2, null);

            Assert.Equal(TestAudience2, payload.Aud?.SingleOrDefault());
        }

        [Fact]
        public async Task ExchangeTokenWithInvalidAudience()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await ExchangeTokenAsync(TestAudience3, null);
            });
        }

        private async Task<JwtPayload> ExchangeTokenAsync(
            string audience,
            TimeSpan? lifetime)
        {
            var tokenClient = CreateSPAuthenticatedClient(TestAppId1);

            var token = await tokenClient.ExchangeAsync(audience, lifetime, CancellationToken.None);

            var payload = DecodeToken(token, new[] { TestDecryptingCredentials1 });
            Assert.Equal(TestIssuer1, payload.Iss);
            Assert.Equal(TestTid, payload[CustomClaims.TenantId] as string);
            Assert.Equal(TestOid, payload[CustomClaims.OId] as string);

            // SP name/email claims should have been filled in via config (appsettings.test.json).
            Assert.Equal("Test Client One", payload[CustomClaims.DisplayName] as string);
            Assert.Equal("test-one@example.com", payload[CustomClaims.Email] as string);
            return payload;
        }

        [Fact]
        public async Task ExchangeTokenWithNonSPToken()
        {
            string name = "Test User";
            string email = "user@example.com";
            var tokenClient = CreateUserAuthenticatedClient(name, email);

            var token = await tokenClient.ExchangeAsync(null, null, CancellationToken.None);

            var payload = DecodeToken(token);
            Assert.Equal(TestIssuer1, payload.Iss);
            Assert.Equal(TestTid, payload[CustomClaims.TenantId] as string);
            Assert.Equal(TestOid, payload[CustomClaims.OId] as string);
            Assert.Equal(name, payload[CustomClaims.DisplayName] as string);
            Assert.Equal(email, payload[CustomClaims.Email] as string);
        }

        [Fact]
        public async Task ExchangeTokenWithTokenInBody()
        {
            var expectedExpiration = DateTime.UtcNow + ExchangeLifetime;

            string name = "Test User";
            string email = "user@example.com";
            var authHeader = await GetUserAuthHeaderAsync(name, email);

            var requestParameters = new ExchangeParameters
            {
                Token = authHeader.Parameter,
            };

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(TokenService.ServiceUri, "/api/v1/tokens/"),
            };

            var response = await httpClient.PostAsJsonAsync(
                "exchange",
                requestParameters,
                CancellationToken.None);

            var result = await TokenServiceClient.ConvertResponseAsync<IssueResult>(
                response, allowNotFound: false, CancellationToken.None);

            var token = result.Token;
            var payload = DecodeToken(token);

            Assert.Equal(TestAudience1, payload.Aud?.SingleOrDefault());

            var exp = payload.ValidTo;
            AssertTime(expectedExpiration, exp);
        }
    }
}
