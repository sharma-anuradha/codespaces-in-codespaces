// <copyright file="IRelayBackplaneManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Interface contract to define a relay backplane manager.
    /// </summary>
    public interface IRelayBackplaneManager :
        IBackplaneManagerBase<IRelayBackplaneProvider, RelayBackplaneProviderSupportLevel, RelayServiceMetrics>,
        IRelayBackplaneProviderBase,
        IRelayBackplaneManagerProvider
    {
        event OnRelayDataChangedAsync<RelayParticipantChanged> ParticipantChangedAsync;

        event OnRelayDataChangedAsync<SendRelayDataHub> SendDataChangedAsync;

        event OnRelayDataChangedAsync<RelayHubChanged> RelayHubChangedAsync;
    }
}
