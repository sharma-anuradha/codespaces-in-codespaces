using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.TokenService.Client;
using Microsoft.VsSaaS.Services.TokenService.Contracts;
using Xunit;

namespace Microsoft.VsSaaS.Services.TokenService.Test
{
    using static TokenServiceFixture;

    public class TokenServiceMetadataTests : TokenServiceTestsBase
    {
        public TokenServiceMetadataTests(TokenServiceFixture tokenService) : base(tokenService)
        {
        }

        [Fact]
        public async Task GetMetadata()
        {
            var issuerName = "testIssuerOne";

            var httpClient = new HttpClient
            {
                BaseAddress = TokenService.ServiceUri,
            };

            var response = await httpClient.GetAsync(
                $"/{issuerName}/.well-known/openid-configuration");

            var result = await TokenServiceClient.ConvertResponseAsync<OpenIdMetadataResult>(
                response, allowNotFound: false, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("TestIssuer1", result.Issuer);
            Assert.Equal(
                TokenService.ServiceUri.AbsoluteUri + "api/v1/certificates?issuer=TestIssuer1",
                result.KeysEndpoint);
        }
    }
}
