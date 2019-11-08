
namespace Microsoft.VsCloudKernel.SignalService
{
    public class AppSettings : AppSettingsBase
    {
        public string BaseUri { get; set; }

        public string AuthenticateProfileServiceUri { get; set; }

        public string BackplaneServiceUri { get; set; }

        public string BackplaneJsonRpcServer { get; set; }

        public string KeyVaultName { get; set; }
    }
}
