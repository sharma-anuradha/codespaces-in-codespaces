using System;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    public class AppSettings
    {
        // General configuration
        public bool IsLocal { get; set; }

        // Key Vault configuration
        public string KeyVaultName { get; set; }

        // Redis configuration
        public string VsClkRedisConnectionString { get; set; }

        // GitHub account configuration
        public string GitHubAppClientId { get; set; }

        public string GitHubAppClientSecret { get; set; }

        // AzureDevOps application configuration
        public string AzDevAppClientId { get; set; }

        public string AzDevAppClientSecret { get; set; }

        public string LiveShareEndpoint { get; set; }

        public string LiveShareWebExtensionEndpoint { get; set; }

        public string PortalEndpoint { get; set; } 
        
        public string EnvironmentRegistrationEndpoint { get; set; }
        public string ApiEndpoint { get; set; }
        public string Domain { get; set; }
        public string AesKey { get; set; }
        public string AesIV { get; set; }
        // The app service principal that has access to reading the app key vault
        public string KeyVaultReaderServicePrincipalClientId { get; set; }
        public string KeyVaultReaderServicePrincipalClientSecret { get; set; }
    }
}
