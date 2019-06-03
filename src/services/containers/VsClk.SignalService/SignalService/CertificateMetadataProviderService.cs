using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.Common.Warmup;
using Newtonsoft.Json;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implements ITokenValidationProvider by using a service Uri which will provide the
    /// authenticate metadata
    /// </summary>
    public class CertificateMetadataProviderService : WarmedUpService, ITokenValidationProvider
    {
        private readonly string certificateMetadataServiceUri;
        private readonly ILogger logger;

        public CertificateMetadataProviderService(
            IList<IAsyncWarmup> warmupServices,
            IList<IHealthStatusProvider> healthStatusProviders,
            string certificateMetadataServiceUri,
            ILogger logger)
            : base(warmupServices, healthStatusProviders)
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
            CompleteWarmup(await UpdateTokenValidationProvider());

            while(true)
            {
                // Every 30 days update the token validation provider
                await Task.Delay(TimeSpan.FromDays(30), cancellationToken);
                await UpdateTokenValidationProvider();
            }
        }

        #endregion

        private async Task<bool> UpdateTokenValidationProvider()
        {
            this.logger.LogInformation($"Retrieving auth metadata from Url:'{this.certificateMetadataServiceUri}'");

            try
            {
                var client = new HttpClient();
                var response = await client.GetAsync(this.certificateMetadataServiceUri);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                var metadata = JsonConvert.DeserializeObject<AuthenticateMetadata>(json);
                Audience = metadata.audience;
                Issuer = metadata.issuer;

                this.logger.LogInformation($"Succesfully receive auth metadata audience:{metadata.audience} issuer:{metadata.issuer} keys count:{metadata.jwtPublicKeys.Length}");
                var securityKeys = new List<SecurityKey>();
                foreach (var item in metadata.jwtPublicKeys)
                {
                    securityKeys.Add(ToSecurityKey(item));
                }

                SecurityKeys = securityKeys.ToArray();
                return true;
            }
            catch (Exception error)
            {
                this.logger.LogError(error, $"Failed to retrieve auth metadata with Url:'{this.certificateMetadataServiceUri}'");
                return false;
            }
        }

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
