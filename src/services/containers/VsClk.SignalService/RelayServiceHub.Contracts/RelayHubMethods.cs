// <copyright file="RelayHubMethods.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Hub Methods defined by relay hub service
    /// </summary>
    public static class RelayHubMethods
    {
        public const string MethodReceiveData = "receiveData";
        public const string MethodParticipantChanged = "participantChanged";
        public const string MethodHubDeleted = "hubDeleted";
    }
}
