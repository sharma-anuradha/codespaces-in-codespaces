using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.VsSaaS.Services.TokenService.Test
{
    using static TokenServiceFixture;

    public class TokenServiceValidateTests : TokenServiceTestsBase
    {
        public TokenServiceValidateTests(TokenServiceFixture tokenService) : base(tokenService)
        {
        }

        [Fact]
        public Task ValidateTokenWithNoEncryption()
        {
            return ValidateTokenAsync(TestAudience1);
        }

        [Fact]
        public Task ValidateTokenWithConfiguredEncryption()
        {
            return ValidateTokenAsync(TestAudience2);
        }

        private async Task ValidateTokenAsync(string audience)
        {
            var token = await IssueTokenAsync(audience);
            Assert.NotNull(token);

            var tokenClient = CreateSPAuthenticatedClient(TestAppId1);
            var payload = await tokenClient.ValidateAsync(token, CancellationToken.None);
            Assert.NotNull(payload);
            Assert.Equal(TestIssuer1, payload.Iss);
            Assert.Equal(audience, payload.Aud?.SingleOrDefault());
        }

        [Fact]
        public async Task ValidateTokenWithInvalidSignature()
        {
            var token = await IssueTokenAsync(TestAudience1);
            Assert.NotNull(token);

            // Cut off the last 3 signature chars.
            token = token.Substring(0, token.Length - 3);

            var tokenClient = CreateSPAuthenticatedClient(TestAppId1);
            await Assert.ThrowsAsync<SecurityTokenInvalidSignatureException>(async () =>
            {
                await tokenClient.ValidateAsync(token, CancellationToken.None);
            });
        }

        [Fact]
        public async Task ValidateTokenExpired()
        {
            var token = await IssueTokenAsync(TestAudience1, DateTime.UtcNow.AddMinutes(-30));
            Assert.NotNull(token);

            var tokenClient = CreateSPAuthenticatedClient(TestAppId1);
            await Assert.ThrowsAsync<SecurityTokenExpiredException>(async () =>
            {
                await tokenClient.ValidateAsync(token, CancellationToken.None);
            });
        }

        [Fact]
        public async Task ValidateTokenNoAuthorization()
        {
            var token = await IssueTokenAsync(TestAudience1);
            Assert.NotNull(token);

            var tokenClient = CreateUnauthenticatedClient();
            var payload = await tokenClient.ValidateAsync(token, CancellationToken.None);
            Assert.NotNull(payload);
            Assert.Equal(TestIssuer1, payload.Iss);
            Assert.Equal(TestAudience1, payload.Aud?.SingleOrDefault());
        }

        [Fact]
        public async Task ValidateTokenEncryptedNoAuthorization()
        {
            var token = await IssueTokenAsync(TestAudience2);
            Assert.NotNull(token);

            var tokenClient = CreateUnauthenticatedClient();
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await tokenClient.ValidateAsync(token, CancellationToken.None);
            });
        }

        [Fact]
        public async Task ValidateUnparsableToken()
        {
            var tokenClient = CreateUnauthenticatedClient();
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await tokenClient.ValidateAsync("notAToken", CancellationToken.None);
            });
        }
    }
}
