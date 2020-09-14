using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Models;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    public class AppSettings
    {
        // General configuration
        public bool IsLocal { get; set; }
        public bool IsTest { get; set; }

        public bool IsDevStamp { get; set; }

        // Key Vault configuration
        public string KeyVaultName { get; set; }

        // Redis configuration
        public string VsClkRedisConnectionString { get; set; }

        // GitHub account configuration
        public string GitHubAppClientId { get; set; }

        public string GitHubAppClientSecret { get; set; }

        // GitHub-created GitHub app to use as an alternative to our 3rd party app,
        // In the future we want to converge to the single app but switching from
        // OAuth app to Github app is very disruptive so deserves a separate PR.
        public string GitHubNativeAppClientId { get; set; }

        public string GitHubNativeAppClientSecret { get; set; }

        // GitHub azure portal configuration
        public string GitHubAzurePortalClientId { get; set; }

        public string GitHubAzurePortalClientSecret { get; set; }

        // AzureDevOps application configuration
        public string AzDevAppClientId { get; set; }

        public string AzDevAppClientSecret { get; set; }

        public string LiveShareEndpoint { get; set; }

        public string LiveShareWebExtensionEndpoint { get; set; }

        public string RichNavWebExtensionEndpoint { get; set; }

        public string PortalEndpoint { get; set; }

        public string EnvironmentRegistrationEndpoint { get; set; }
        public string ApiEndpoint { get; set; }
        public string Domain { get; set; }
        public string AesKey { get; set; }

        public string AesIV { get; set; }

        // The app service principal that has access to reading the app key vault
        public string KeyVaultReaderServicePrincipalClientId { get; set; }
        public string KeyVaultReaderServicePrincipalClientSecret { get; set; }
        public string VsSaaSCertificateSecretName { get; set; }
        public string VsSaaSTokenIssuer { get; set; }
        public string VsSaaSTokenCertsEndpoint { get; set; }

        public IEnumerable<HostsConfig> PortForwardingHostsConfigs { get; set; }
        public string PortForwardingDomainTemplate { get; set; }
        public string GitHubPortForwardingDomainTemplate { get; set; }

        public string PortForwardingServiceEnabled { get; set; }
        public string GitHubPortForwardingServiceEnabled { get; set; }
        public string PortForwardingEnableEnvironmentEndpoints { get; set; }
        public string GitHubportForwardingEnableEnvironmentEndpoints { get; set; }
        public string PortForwardingManagementEndpoint { get; set; }
        public string GitHubPortForwardingManagementEndpoint { get; set; }
    }
}