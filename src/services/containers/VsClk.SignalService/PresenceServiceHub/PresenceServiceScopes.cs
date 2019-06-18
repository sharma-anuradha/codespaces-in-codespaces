using System;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Definition of our scopes for the presence service
    /// </summary>
    internal static class PresenceServiceScopes
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
        public const string ConnectionScope = "Connection";

        public static IDisposable BeginContactReferenceScope(this ILogger logger, string method, ContactReference contactReference)
        {
            return logger.BeginScope(
                    (MethodScope, method),
                    (ContactScope, contactReference.Id),
                    (ConnectionScope, contactReference.ConnectionId));
        }
    }
}
