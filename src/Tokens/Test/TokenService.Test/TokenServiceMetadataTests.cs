using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.VsSaaS.Services.TokenService.Client;
using Microsoft.VsSaaS.Services.TokenService.Contracts;
using Xunit;

namespace Microsoft.VsSaaS.Services.TokenService.Test
{
    using static TokenServiceFixture;

    public class TokenServiceMetadataTests : TokenServiceTestsBase
    {
        private const string TestIssuer1 = "testIssuerOne";

        private readonly HttpClient httpClient;

        public TokenServiceMetadataTests(TokenServiceFixture tokenService) : base(tokenService)
        {
            this.httpClient = new HttpClient
            {
                BaseAddress = TokenService.ServiceUri,
            };
        }

        [Fact]
        public async Task GetMetadata()
        {
            var response = await this.httpClient.GetAsync(
                $"/{TestIssuer1}/.well-known/openid-configuration");

            var result = await TokenServiceClient.ConvertResponseAsync<OpenIdMetadataResult>(
                response, allowNotFound: false, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("TestIssuer1", result.Issuer);
            Assert.Equal(
                TokenService.ServiceUri.AbsoluteUri + "api/v1/certificates?issuer=TestIssuer1",
                result.KeysEndpoint);
        }

        [Fact]
        public async Task ConfigureMetadata()
        {
            var retriever = new HttpDocumentRetriever(this.httpClient);
            retriever.RequireHttps = false;
            var config = await OpenIdConnectConfigurationRetriever.GetAsync(
                $"/{TestIssuer1}/.well-known/openid-configuration",
                retriever,
                CancellationToken.None);
            Assert.NotNull(config);
            Assert.Equal("TestIssuer1", config.Issuer);
        }
    }
}
