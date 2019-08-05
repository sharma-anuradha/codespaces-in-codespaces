namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Definition of our scopes for the relay service
    /// </summary>
    internal static class RelayServiceScopes
    {
        public const string MethodCreateHub = "CreateHub";
        public const string MethodJoinHub = "JoinHub";
        public const string MethodLeaveHub = "LeaveHub";
        public const string MethodSendDataHub = "SendDataHub";
    }
}
