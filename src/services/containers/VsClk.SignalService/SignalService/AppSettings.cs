
namespace Microsoft.VsCloudKernel.SignalService
{
    public class AppSettings
    {
        public string BuildVersion { get; set; }

        public string GitCommit { get; set; }

        public string GitBranch { get; set; }

        public bool UseTelemetryProvider { get; set; }

        public string AuthenticateMetadataServiceUri { get; set; }

        public string AzureCosmosDbEndpointUrl { get; set; }

        public string AzureCosmosDbAuthKey { get; set; }
    }
}
