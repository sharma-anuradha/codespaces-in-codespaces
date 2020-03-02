// <copyright file="RelayServiceScopes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Definition of our scopes for the relay service.
    /// </summary>
    internal static class RelayServiceScopes
    {
        public const string MethodCreateHub = "CreateHub";
        public const string MethodDeleteHub = "DeleteHub";
        public const string MethodJoinHub = "JoinHub";
        public const string MethodUpdateHub = "UpdateHub";
        public const string MethodLeaveHub = "LeaveHub";
        public const string MethodSendDataHub = "SendDataHub";
        public const string MethodDisconnectHub = "DisconnectHub";
    }
}
