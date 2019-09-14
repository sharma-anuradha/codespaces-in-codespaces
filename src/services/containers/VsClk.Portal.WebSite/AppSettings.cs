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

        // Github account configuration
        public string GithubAppClientId { get; set; }

        public string GithubAppClientSecret { get; set; }

        public string EnvironmentRegistrationEndpoint { get; set; }
        public string Domain { get; set; }
        public string AesKey { get; set; }
        public string AesIV { get; set; }
    }
}
