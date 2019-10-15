namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    public enum PortForwardingFailure {
        Unknown,
        InvalidPath,
        InvalidCookiePayload,
        NotAuthenticated,
        NotAuthorized,
        InvalidWorkspaceOrOwner
    }

    public class PortForwardingErrorDetails
    {
        public PortForwardingFailure FailureReason { get; set; }
        public string RedirectUrl { get; set; }
    }
}
