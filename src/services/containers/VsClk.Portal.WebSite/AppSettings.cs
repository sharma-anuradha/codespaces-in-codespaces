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

        // Microsoft account configuration
        public string MicrosoftAppClientId { get; set; }

        public string MicrosoftAppClientSecret { get; set; }

        // GitHub account configuration
        public string GitHubAppClientId { get; set; }

        public string GitHubAppClientSecret { get; set; }


        public string EnvironmentRegistrationEndpoint { get; set; }
        public string Domain { get; set; }
        public string AesKey { get; set; }
        public string AesIV { get; set; }
    }
}
