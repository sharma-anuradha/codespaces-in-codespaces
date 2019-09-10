using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Moq;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class ServicePrincipalTests
    {
        [Fact]
        public void ConstructorThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new ServicePrincipal(default(IOptions<ServicePrincipalOptions>), GetMockSecretProvider()));
            Assert.Throws<ArgumentNullException>(() => new ServicePrincipal(default(ServicePrincipalSettings), GetMockSecretProvider()));
            Assert.Throws<ArgumentNullException>(() => new ServicePrincipal(GetServicePrincipalSettings(), null));
        }

        [Fact]
        public void ConstructorOK()
        {
            var servicePrincipal = new ServicePrincipal(GetServicePrincipalSettings(), GetMockSecretProvider());
            Assert.Equal("client_id", servicePrincipal.ClientId);
            Assert.Equal("tenant_id", servicePrincipal.TenantId);

            servicePrincipal = new ServicePrincipal("client_id", "secret_name", "tenant_id", GetMockSecretProvider());
            Assert.Equal("client_id", servicePrincipal.ClientId);
            Assert.Equal("tenant_id", servicePrincipal.TenantId);
        }

        [Fact]
        public async Task GetServicePrincipalClientSecret()
        {
            var servicePrincipal = new ServicePrincipal(GetServicePrincipalSettings(), GetMockSecretProvider());
            var value = await servicePrincipal.GetServicePrincipalClientSecretAsync();
            Assert.Equal("secret_value", value);
        }

        private static ServicePrincipalSettings GetServicePrincipalSettings()
        {
            return new ServicePrincipalSettings
            {
                ClientId = "client_id",
                ClientSecretName = "secret_name",
                TenantId = "tenant_id"
            };
        }

        private static ISecretProvider GetMockSecretProvider()
        {
            var secretsProviderMoq = new Mock<ISecretProvider>();
            secretsProviderMoq
                .Setup(sp => sp.GetSecretAsync("secret_name"))
                .ReturnsAsync("secret_value");

            return secretsProviderMoq.Object;
        }
    }
}
