namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    public class Constants
    {
        public static string LiveShareTokenExchangeRoute = "/auth/exchange";
        public static string LiveShareWorkspaceRoute = "/api/v1.2/workspace/";

        public static int PortForwarderCookieExpirationDays = 2;
        public static string PFCookieName = "__Host-vso-pf";

        public static string AzureDevOpsTokenURL = "https://app.vssps.visualstudio.com/oauth2/token";
        public static string AzureDevOpsAuthorizeURL = "https://app.vssps.visualstudio.com/oauth2/authorize";
    }
}
