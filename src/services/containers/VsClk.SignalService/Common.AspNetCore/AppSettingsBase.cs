
namespace Microsoft.VsCloudKernel.SignalService
{
    public class AppSettingsBase
    {
        public string Stamp { get; set; }

        public string ImageTag { get; set; }

        public bool UseTelemetryProvider { get; set; }

        public string AzureRedisConnection { get; set; }

        public string AzureCosmosDbEndpointUrl { get; set; }

        public string AzureCosmosDbAuthKey { get; set; }

        public bool IsPrivacyEnabled { get; set; }

        public bool IsAzureDocumentsProviderEnabled { get; set; }

        public string SubscriptionId { get; set; }
        public string ResourceGroupName { get; set; }
        public string ResourceGroupInstanceName { get; set; }
        public string AzureCacheRedisName { get; set; }
        public string AzureCosmosDbName { get; set; }
    }

    public class ApplicationServicePrincipal
    {
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public string ClientPassword { get; set; }
    }
}
