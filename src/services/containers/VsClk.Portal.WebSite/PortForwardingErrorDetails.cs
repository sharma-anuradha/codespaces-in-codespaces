namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    public enum PortForwardingFailure
    {
        Unknown,
        InvalidCookiePayload,
        NotAuthenticated,
        NotAuthorized,
        InvalidWorkspaceOrOwner,
        None,
    }

    public class PortForwardingErrorDetails
    {
        public PortForwardingFailure FailureReason { get; set; }
        public string RedirectUrl { get; set; }
    }
}
