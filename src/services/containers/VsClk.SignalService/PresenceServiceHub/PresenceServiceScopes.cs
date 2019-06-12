namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Definition of our scopes for the presence service
    /// </summary>
    internal class PresenceServiceScopes
    {
        public const string MethodScope = "Method";

        public const string MethodRegisterSelfContact = "RegisterSelfContact";
        public const string MethodRequestSubcriptions = "RequestSubcriptions";
        public const string MethodAddSubcriptions = "AddSubcriptions";
        public const string MethodRemoveSubscription = "RemoveSubscription";
        public const string MethodUpdateProperties = "UpdateProperties";
        public const string MethodSendMessage = "SendMessage";
        public const string MethodUnregisterSelfContact = "UnregisterSelfContact";
        public const string MethodMatchContacts = "MatchContacts";

        public const string ContactScope = "Contact";
    }
}
