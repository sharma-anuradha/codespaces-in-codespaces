// <copyright file="ContactServiceScopes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Definition of our scopes for the presence service
    /// </summary>
    internal static class ContactServiceScopes
    {
        public const string MethodRegisterSelfContact = "RegisterSelfContact";
        public const string MethodRequestSubcriptions = "RequestSubcriptions";
        public const string MethodAddSubcriptions = "AddSubcriptions";
        public const string MethodRemoveSubscription = "RemoveSubscription";
        public const string MethodUpdateProperties = "UpdateProperties";
        public const string MethodSendMessage = "SendMessage";
        public const string MethodOnMessageReceived = "OnMessageReceived";
        public const string MethodUnregisterSelfContact = "UnregisterSelfContact";
        public const string MethodMatchContacts = "MatchContacts";
        public const string MethodContactOnContactChanged = "Contact.OnContactChanged";

        public const string ContactScope = "Contact";
        public const string ConnectionScope = "Connection";
        public const string MessageTypeScope = "MessageType";

        public static IDisposable BeginContactReferenceScope(this ILogger logger, string method, ContactReference contactReference, IFormatProvider formatProvider, params (string, object)[] scopes)
        {
            return BeginContactReferenceScope(logger, method, contactReference.Id, contactReference.ConnectionId, formatProvider, scopes);
        }

        public static IDisposable BeginContactReferenceScope(this ILogger logger, string method, string contactId, string connectionId, IFormatProvider formatProvider, params (string, object)[] scopes)
        {
            var allScopes = new List<(string, object)>()
            {
                (LoggerScopeHelpers.MethodScope, method),
                (ContactScope, string.Format(formatProvider, "{0:T}", contactId)),
                (ConnectionScope, connectionId),
            };

            if (scopes != null)
            {
                allScopes.AddRange(scopes);
            }

            return LoggerScopeHelpers.BeginScope(logger, allScopes.ToArray());
        }
    }
}
