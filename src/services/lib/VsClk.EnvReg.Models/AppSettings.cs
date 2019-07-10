using System;
using System.Runtime.Serialization;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models
{
    public class AppSettings
    {
        // General configuration
        public string SystemProtocol { get; set; }

        public string SystemHost { get; set; }

        public string BuildVersion { get; set; }

        public string GitCommit { get; set; } = "local";

        public string GitBranch { get; set; }

        public string AzureLocation { get; set; }

#if DEBUG
        // This option is only available on Debug Builds, just to make a clear distinction between IsDevelopment.
        public bool UseMocksForLocalDevelopment { get; set; }

        public bool UseLocalDockerForComputeProvisioning { get; set; }

        public string DockerImage { get; set; }
#endif // DEBUG

        // Authentication configuration
        public string AuthJwtAudience { get; set; }

        public string AuthJwtAudiences { get; set; }

        public string AuthJwtAuthority { get; set; }

        // Key Vault configuration
        public string KeyVaultName { get; set; }

        // Redis configuration
        public string VsClkRedisConnectionString { get; set; }

        // DocDb configuration
        public string VsClkEnvRegDbHost { get; set; }

        public string VsClkEnvRegDbKey { get; set; }

        public string VsClkEnvRegDbId { get; set; }

        public string VsClkEnvRegPreferredLocation { get; set; }

        public string StampLocation { get; set; }
        public string ComputeServiceUrl { get; set; }
        public Uri ComputeServiceUri { get { return new Uri(this.ComputeServiceUrl); } }

        public string PreferredSchema { get; set; }

        public string DefaultHost { get; set; }

        public string DefaultPath { get; set; }

        public string StorageAccountName { get; set; }

        public string StorageAccountKey { get; set; }

        // Live Share Services
        public string VSLiveShareApiEndpoint { get; set; }
    }
}
