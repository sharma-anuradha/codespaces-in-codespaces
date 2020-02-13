// <copyright file="IRelayBackplaneServiceNotification.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.SignalService;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    public interface IRelayBackplaneServiceNotification
    {
        Task FireSendDataHubAsync(SendRelayDataHub dataChanged, CancellationToken cancellationToken);

        Task FireNotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken);

        Task FireNotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken);
    }
}
