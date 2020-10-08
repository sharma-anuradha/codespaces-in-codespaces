using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.TokenService.Contracts;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.TokenService.Test
{
    using static TokenServiceFixture;

    public class TokenServiceAnonymousTests : TokenServiceTestsBase
    {
        public TokenServiceAnonymousTests(TokenServiceFixture tokenService) : base(tokenService)
        {
        }

        [Fact]
        public async Task DefaultAnonymousTokenCanBeCreatedWithoutParams()
        {
            var expectedExpiration = DateTime.UtcNow.Add(AnonymousLifetime);

            var payload = await AnonymousTokenAsync();

            Assert.NotNull(payload);
            AssertTime(expectedExpiration, payload.ValidTo);
            Assert.Equal(TestAudience1, payload.Aud?.SingleOrDefault());
        }

        [Fact]
        public async Task CreateAnonymousTokenIncludesProvidedDisplayName()
        {
            var expectedDisplayName = "custom_name";

            var payload = await AnonymousTokenAsync(expectedDisplayName);

            Assert.Equal(expectedDisplayName, payload[CustomClaims.DisplayName]);
        }

        [Fact]
        public async Task EmptyDisplayNameIsAllowed()
        {
            var displayName = "";

            var payload = await AnonymousTokenAsync(displayName);

            Assert.False(payload.ContainsKey(CustomClaims.DisplayName));
        }

        [Fact]
        public async Task LongerDisplayNameIsTruncated()
        {
            var displayName = "This is a very long display name and should be truncated";
            var expectedDisplayName = displayName.Substring(0, AnonymousDisplayNameMaxLength);

            var payload = await AnonymousTokenAsync(displayName);

            Assert.Equal(expectedDisplayName, payload[CustomClaims.DisplayName]);
        }

        [Fact]
        public async Task DisplayNameIsWhitespaceTrimmed()
        {
            var displayName = "    display name    ";
            var expectedDisplayName = "display name";

            var payload = await AnonymousTokenAsync(displayName);

            Assert.Equal(expectedDisplayName, payload[CustomClaims.DisplayName]);
        }

        [Fact]
        public async Task SomeSpecialCharsAreRemovedFromDisplayName()
        {
            var displayName = " di;s&pla?y<n==a<m&&e>    ";
            var expectedDisplayName = "displayname";

            var payload = await AnonymousTokenAsync(displayName);

            Assert.Equal(expectedDisplayName, payload[CustomClaims.DisplayName]);
        }

        [Fact]
        public async Task CreateAnonymousTokenUsesIssuerMaxExpiration()
        {
            var expectedExpiration = DateTime.UtcNow.Add(AnonymousLifetime);

            var payload = await AnonymousTokenAsync("name");

            AssertTime(expectedExpiration, payload.ValidTo);
        }

        [Fact]
        public async Task CreateAnonymousTokenHasAnonymousClaim()
        {
            var expectedAnonymousClaimValue = "anonymous";

            var payload = await AnonymousTokenAsync("displayName");

            Assert.Equal(expectedAnonymousClaimValue, payload[CustomClaims.Anonymous].ToString());
        }

        [Fact]
        public async Task AnonymousTokenWithSpecifiedAudience()
        {
            var payload = await AnonymousTokenAsync(audience: TestAudience2);

            Assert.Equal(TestAudience2, payload.Aud?.SingleOrDefault());
        }

        [Fact]
        public async Task AnonymousTokenWithInvalidAudience()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await AnonymousTokenAsync(audience: TestAudience3);
            });
        }

        [Fact]
        public async Task AnonymousTokenWithSpecifiedLifetime()
        {
            var testLifetime = TimeSpan.FromMinutes(30);
            var expectedExpiration = DateTime.UtcNow + testLifetime;

            var payload = await AnonymousTokenAsync(lifetime: testLifetime);
            var exp = payload.ValidTo;
            AssertTime(expectedExpiration, exp);
        }

        [Fact]
        public async Task AnonymousTokenWithMaximumLifetime()
        {
            var testLifetime = TimeSpan.FromDays(100);
            var expectedExpiration = DateTime.UtcNow + ExchangeLifetime;

            var payload = await AnonymousTokenAsync(lifetime: testLifetime);
            var exp = payload.ValidTo;
            AssertTime(expectedExpiration, exp);
        }


        private async Task<JwtPayload> AnonymousTokenAsync(
            string displayName = null,
            string audience = null,
            TimeSpan? lifetime = null)
        {
            var tokenClient = CreateUnauthenticatedClient();

            var token = await tokenClient.CreateAnonymousTokenAsync(
                (displayName == null && audience==null && lifetime == null) ?
                null :
                new AnonymousParameters
                {
                    DisplayName = displayName,
                    Audience = audience,
                    Lifetime = lifetime
                },
                CancellationToken.None);

            var payload = DecodeToken(token, new[] { TestDecryptingCredentials1 });

            Assert.Equal(TestIssuer2, payload.Iss);
            return payload;
        }
    }
}
