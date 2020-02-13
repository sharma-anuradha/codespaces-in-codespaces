// <copyright file="IRelayServiceClientHub.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Contract definition for the relay client notifications
    /// </summary>
    public interface IRelayServiceClientHub
    {
        Task ReceiveDataAsync(
            string hubId,
            string fromParticipantId,
            int uniqueId,
            string type,
            byte[] data);

        Task ParticipantChangedAsync(
            string hubId,
            string participantId,
            Dictionary<string, object> properties,
            ParticipantChangeType changeType);

        Task HubDeletedAsync(
            string hubId);
    }
}
