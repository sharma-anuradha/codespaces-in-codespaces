
namespace Microsoft.VsCloudKernel.SignalService
{
    public class AppSettings
    {
        public string Stamp { get; set; }

        public string BaseUri { get; set; }

        public string ImageTag { get; set; }

        public bool UseTelemetryProvider { get; set; }

        public string AuthenticateMetadataServiceUri { get; set; }

        public string AzureCosmosDbEndpointUrl { get; set; }

        public string AzureCosmosDbAuthKey { get; set; }
    }
}
