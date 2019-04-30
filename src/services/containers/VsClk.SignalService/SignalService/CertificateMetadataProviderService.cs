using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implements ITokenValidationProvider by using a service Uri which will provide the
    /// authenticate metadata
    /// </summary>
    public class CertificateMetadataProviderService : BackgroundService, ITokenValidationProvider
    {
        private readonly string certificateMetadataServiceUri;
        private readonly ILogger logger;

        public CertificateMetadataProviderService(
            string certificateMetadataServiceUri,
            ILogger logger)
        {
            Requires.NotNullOrEmpty(certificateMetadataServiceUri, nameof(certificateMetadataServiceUri));
            this.certificateMetadataServiceUri = certificateMetadataServiceUri;
            this.logger = logger;
            SecurityKeys = Array.Empty<SecurityKey>();
        }

        #region ITokenValidationProvider

        public string Audience { get; private set; }
        public string Issuer { get; private set; }
        public SecurityKey[] SecurityKeys { get; private set; }

        #endregion

        #region BackgroundService Override

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"Retrieving auth metadata from Url:'{this.certificateMetadataServiceUri}'");

            var client = new HttpClient();
            var response = await client.GetAsync(this.certificateMetadataServiceUri);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            var metadata = JsonConvert.DeserializeObject<AuthenticateMetadata>(json);
            Audience = metadata.audience;
            Issuer = metadata.issuer;

            this.logger.LogInformation($"Succesfully receive auth metadata audience:{metadata.audience} issuer:{metadata.issuer} keys count:{metadata.jwtPublicKeys.Length}");
            var securityKeys = new List<SecurityKey>();
            foreach(var item in metadata.jwtPublicKeys)
            {
                securityKeys.Add(ToSecurityKey(item));
            }

            SecurityKeys = securityKeys.ToArray();
        }

        #endregion

        private static SecurityKey ToSecurityKey(string base64Data)
        {
            var bytes = Convert.FromBase64String(base64Data);
            var certficate2 = new X509Certificate2(bytes);
            return new X509SecurityKey(certficate2);
        }

#pragma warning disable IDE1006 // Naming Styles

        private class AuthenticateMetadata
        {
            public string audience { get; set; }
            public string issuer { get; set; }
            public string[] jwtPublicKeys { get; set; }
        }
    }
#pragma warning restore IDE1006 // Naming Styles

}
