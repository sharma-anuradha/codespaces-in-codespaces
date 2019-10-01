
namespace Microsoft.VsCloudKernel.SignalService
{
    public class AppSettings
    {
        public string Stamp { get; set; }

        public string BaseUri { get; set; }

        public string ImageTag { get; set; }

        public bool UseTelemetryProvider { get; set; }

        public bool IsPrivacyEnabled { get; set; }

        public string AuthenticateProfileServiceUri { get; set; }

        public string AzureCosmosDbEndpointUrl { get; set; }

        public string AzureCosmosDbAuthKey { get; set; }

        public bool IsAzureDocumentsProviderEnabled { get; set; }

        public string AzureRedisConnection { get; set; }

        public string KeyVaultName { get; set; }
    }

    public class ApplicationServicePrincipal
    {
        public string ClientId { get; set; }
        public string ClientPassword { get; set; }
    }
}
