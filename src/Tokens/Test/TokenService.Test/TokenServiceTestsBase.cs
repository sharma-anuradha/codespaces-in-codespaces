using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.TokenService.Client;
using Microsoft.VsSaaS.Tokens;
using Xunit;
using Xunit.Extensions.AssemblyFixture;

namespace Microsoft.VsSaaS.Services.TokenService.Test
{
    using static TokenServiceFixture;

    /// <summary>
    /// Base class for integration tests that use a <see cref="TokenServiceClient" /> to
    /// call APIs in the token service that is launched as a class fixture.
    /// </summary>
    public abstract class TokenServiceTestsBase : IAssemblyFixture<TokenServiceFixture>
    {
        protected static readonly string TestTid = Guid.Empty.ToString();
        protected static readonly string TestOid = TestTid.Replace("0", "1");

        public TokenServiceTestsBase(TokenServiceFixture tokenService)
        {
            TokenService = tokenService;
        }

        public TokenServiceFixture TokenService { get; }

        protected TokenServiceClient CreateSPAuthenticatedClient(string appId)
        {
            return new TokenServiceClient(
                new HttpClient { BaseAddress = TokenService.ServiceUri },
                () => GetSPAuthHeaderAsync(appId));
        }

        protected static Task<AuthenticationHeaderValue> GetSPAuthHeaderAsync(string appId)
        {
            var payload = new JwtPayload(new[]
            {
                new Claim("appid", appId),
                new Claim(CustomClaims.TenantId, TestTid),
                new Claim(CustomClaims.OId, TestOid),
            });
            var mockToken = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(payload.SerializeToJson()));
            return Task.FromResult(new AuthenticationHeaderValue("Bearer", mockToken));
        }

        protected TokenServiceClient CreateUserAuthenticatedClient(string name, string email)
        {
            return new TokenServiceClient(
                new HttpClient { BaseAddress = TokenService.ServiceUri },
                () => GetUserAuthHeaderAsync(name, email));
        }

        protected static Task<AuthenticationHeaderValue> GetUserAuthHeaderAsync(
            string name, string email)
        {
            string mockToken = CreateMockToken(name, email);
            return Task.FromResult(new AuthenticationHeaderValue("Bearer", mockToken));
        }

        protected static string CreateMockToken(string name, string email)
        {
            var payload = new JwtPayload(new[]
            {
                new Claim(CustomClaims.DisplayName, name),
                new Claim(CustomClaims.Email, email),
                new Claim(CustomClaims.TenantId, TestTid),
                new Claim(CustomClaims.OId, TestOid),
            });
            var mockToken = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(payload.SerializeToJson()));
            return mockToken;
        }

        protected TokenServiceClient CreateUnauthenticatedClient()
        {
            return new TokenServiceClient(new HttpClient { BaseAddress = TokenService.ServiceUri });
        }

        protected static JwtPayload DecodeToken(
            string token,
            IEnumerable<EncryptingCredentials> encryptingCredentials = null)
        {
            return new JwtReader().DecodeToken(token, encryptingCredentials);
        }

        protected static void AssertTime(DateTime expected, DateTime actual)
        {
            Assert.True(expected.AddSeconds(-1) < actual,
                $"Expected: {expected:s}, Actual: {actual:s}");
            Assert.True(expected.AddMinutes(5) > actual,
                $"Expected: {expected:s}, Actual: {actual:s}");
        }

        protected async Task<string> IssueTokenAsync(
            string audience,
            DateTime? expires = null)
        {
            var tokenClient = new TokenServiceClient(
                new HttpClient { BaseAddress = TokenService.ServiceUri },
                () => GetSPAuthHeaderAsync(TestAppId1));

            var payload = new JwtPayload(
                TestIssuer1,
                audience,
                Enumerable.Empty<Claim>(),
                notBefore: DateTime.UtcNow.AddHours(-1),
                expires ?? DateTime.UtcNow.AddHours(1));

            var token = await tokenClient.IssueAsync(payload, CancellationToken.None);

            return token;
        }
    }
}
