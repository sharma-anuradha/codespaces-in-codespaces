using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Services.TokenService.Client;
using Xunit;

namespace Microsoft.VsSaaS.Services.TokenService.Test
{
    using static TokenServiceFixture;

    public class TokenServiceIssueTests : TokenServiceTestsBase
    {
        public TokenServiceIssueTests(TokenServiceFixture tokenService) : base(tokenService)
        {
        }

        [Fact]
        public async Task IssueTokenWithDefaults()
        {
            var tokenClient = new TokenServiceClient(
                TokenService.ServiceUri, () => GetSPAuthHeaderAsync(TestAppId1));

            var payload = new JwtPayload();
            payload.AddClaim(new Claim(JwtRegisteredClaimNames.Aud, TestAudience1));

            var token = await tokenClient.IssueAsync(payload, CancellationToken.None);
            Assert.NotNull(token);

            var result = DecodeToken(token);
            Assert.Equal(TestIssuer1, result.Iss);
            Assert.Equal(TestAudience1, result.Aud?.SingleOrDefault());
            Assert.NotNull(result.Exp);
        }

        [Fact]
        public async Task IssueTokenWithNoEncryption()
        {
            var token = await IssueTokenAsync(TestAudience1);
            Assert.NotNull(token);

            var payload = DecodeToken(token);
            Assert.Equal(TestIssuer1, payload.Iss);
            Assert.Equal(TestAudience1, payload.Aud?.SingleOrDefault());
        }

        [Fact]
        public async Task IssueTokenWithConfiguredEncryptionCert()
        {
            var token = await IssueTokenAsync(TestAudience2);
            Assert.NotNull(token);

            var audiencePrivateCert = GetTestCert("TestAudience", isPrivate: true);

            var header = DecodeToken(token);
            Assert.Equal(audiencePrivateCert.Thumbprint, header["kid"] as string);

            var payload = DecodeToken(
                token, new[] { new X509EncryptingCredentials(audiencePrivateCert) });
            Assert.Equal(TestIssuer1, payload.Iss);
            Assert.Equal(TestAudience2, payload.Aud?.SingleOrDefault());
        }

        [Fact]
        public async Task IssueTokenAccessForbidden()
        {
            // TestAppId2 should not have permission to issue tokens.
            var tokenClient = CreateSPAuthenticatedClient(TestAppId2);

            var payload = new JwtPayload(
                TestIssuer1,
                TestAudience1,
                Enumerable.Empty<Claim>(),
                notBefore: null,
                expires: DateTime.UtcNow.AddHours(1));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await tokenClient.IssueAsync(payload, CancellationToken.None);
            });
        }

        [Fact]
        public async Task IssueTokenWithInvalidIssuer()
        {
            // TestAppId1 should not have permission to issue tokens for TestIssuer2.
            var tokenClient = CreateSPAuthenticatedClient(TestAppId1);

            var payload = new JwtPayload(
                TestIssuer2,
                TestAudience1,
                Enumerable.Empty<Claim>(),
                notBefore: null,
                expires: DateTime.UtcNow.AddHours(1));

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await tokenClient.IssueAsync(payload, CancellationToken.None);
            });
        }

        [Fact]
        public async Task IssueTokenWithInvalidAudience()
        {
            // TestAppId1 should not have permission to issue tokens for TestAudience3.
            var tokenClient = CreateSPAuthenticatedClient(TestAppId1);

            var payload = new JwtPayload(
                TestIssuer1,
                TestAudience3,
                Enumerable.Empty<Claim>(),
                notBefore: null,
                expires: DateTime.UtcNow.AddHours(1));

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await tokenClient.IssueAsync(payload, CancellationToken.None);
            });
        }

        [Fact]
        public async Task IssueTokenWithInvalidExpiration()
        {
            var tokenClient = CreateSPAuthenticatedClient(TestAppId1);

            var payload = new JwtPayload(
                TestIssuer1,
                TestAudience1,
                Enumerable.Empty<Claim>(),
                notBefore: null,
                expires: DateTime.UtcNow.AddHours(-1));

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await tokenClient.IssueAsync(payload, CancellationToken.None);
            });
        }

        [Fact]
        public async Task IssueTokenNoAuthorization()
        {
            var tokenClient = CreateUnauthenticatedClient();

            var payload = new JwtPayload(
                TestIssuer1,
                TestAudience1,
                Enumerable.Empty<Claim>(),
                notBefore: null,
                expires: DateTime.UtcNow.AddHours(1));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await tokenClient.IssueAsync(payload, CancellationToken.None);
            });
        }
    }
}
