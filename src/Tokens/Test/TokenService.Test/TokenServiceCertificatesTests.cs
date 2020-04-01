using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.TokenService.Test
{
    using static TokenServiceFixture;

    public class TokenServiceCertificatesTests : TokenServiceTestsBase
    {
        public TokenServiceCertificatesTests(TokenServiceFixture tokenService) : base(tokenService)
        {
        }

        [Fact]
        public async Task GetPublicCertificates()
        {
            var tokenClient = CreateUnauthenticatedClient();
            var results = await tokenClient.GetIssuerPublicCertificatesAsync(
                TestIssuer1, CancellationToken.None);
            Assert.NotNull(results);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task GetPublicCertificatesIssuerNotFound()
        {
            var tokenClient = CreateUnauthenticatedClient();
            var results = await tokenClient.GetIssuerPublicCertificatesAsync(
                "TestIssuer0", CancellationToken.None);
            Assert.Null(results);
        }
    }
}
