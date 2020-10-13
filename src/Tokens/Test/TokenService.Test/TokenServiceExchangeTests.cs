using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
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

            var token = await tokenClient.ExchangeAsync(
                new ExchangeParameters
                {
                    Audience = audience,
                    Lifetime = lifetime,
                },
                CancellationToken.None);

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

            var token = await tokenClient.ExchangeAsync(
                new ExchangeParameters(), CancellationToken.None);

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
            string name = "Test User";
            string email = "user@example.com";
            var tokenClient = CreateUnauthenticatedClient();

            var token = await tokenClient.ExchangeAsync(
                new ExchangeParameters
                {
                    Token = CreateMockToken(name, email),
                    Provider = ProviderNames.Microsoft,
                },
                CancellationToken.None);

            var payload = DecodeToken(token);
            Assert.Equal(TestIssuer1, payload.Iss);
            Assert.Equal(TestTid, payload[CustomClaims.TenantId] as string);
            Assert.Equal(TestOid, payload[CustomClaims.OId] as string);
            Assert.Equal(name, payload[CustomClaims.DisplayName] as string);
            Assert.Equal(email, payload[CustomClaims.Email] as string);
        }

        [Fact]
        public async Task ExchangeTokenWithScopeHandler()
        {
            string name = "Test User";
            string email = "user@example.com";
            var tokenClient = CreateUserAuthenticatedClient(name, email);

            var token = await tokenClient.ExchangeAsync(
                new ExchangeParameters
                {
                    Scopes = new[] { "test" },
                },
                CancellationToken.None);

            var payload = DecodeToken(token);
            var scopeClaim = payload.Claims.FirstOrDefault((c) => c.Type == CustomClaims.Scope);
            Assert.Equal("test", scopeClaim?.Value);
        }

        [Fact]
        public async Task ExchangeTokenWithInvalidScope()
        {
            string name = "Test User";
            string email = "user@example.com";
            var tokenClient = CreateUserAuthenticatedClient(name, email);

            var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await tokenClient.ExchangeAsync(
                    new ExchangeParameters
                    {
                        Scopes = new[] { "invalidscope" },
                    },
                    CancellationToken.None);
            });
            Assert.Contains("invalidscope", ex.Message);
        }
    }
}
