// <copyright file="RelayBackplaneServiceProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    public class RelayBackplaneServiceProvider : BackplaneServiceProviderBase, IRelayBackplaneProvider
    {
        public RelayBackplaneServiceProvider(
            IBackplaneConnectorProvider backplaneConnectorProvider,
            string hostServiceId,
            CancellationToken stoppingToken)
            : base(backplaneConnectorProvider, hostServiceId, stoppingToken)
        {
            Func<SendRelayDataHub, CancellationToken, Task> onFireSendDataCallback = (dataChanged, ct) =>
            {
                return FireSendDataHubAsync(dataChanged, ct);
            };

            backplaneConnectorProvider.AddTarget(nameof(FireSendDataHubAsync), onFireSendDataCallback);

            Func<RelayParticipantChanged, CancellationToken, Task> onFireNotifyParticipantaCallback = (dataChanged, ct) =>
            {
                return FireNotifyParticipantChangedAsync(dataChanged, ct);
            };

            backplaneConnectorProvider.AddTarget(nameof(FireNotifyParticipantChangedAsync), onFireNotifyParticipantaCallback);

            Func<RelayHubChanged, CancellationToken, Task> onFireNotifyRelayHubChangedCallback = (dataChanged, ct) =>
            {
                return FireNotifyRelayHubChangedAsync(dataChanged, ct);
            };

            backplaneConnectorProvider.AddTarget(nameof(FireNotifyRelayHubChangedAsync), onFireNotifyRelayHubChangedCallback);
        }

        /// <inheritdoc/>
        public OnRelayDataChangedAsync<RelayParticipantChanged> ParticipantChangedAsync { get; set; }

        /// <inheritdoc/>
        public OnRelayDataChangedAsync<SendRelayDataHub> SendDataChangedAsync { get; set; }

        /// <inheritdoc/>
        public OnRelayDataChangedAsync<RelayHubChanged> RelayHubChanged { get; set; }

        /// <inheritdoc/>
        protected override string ServiceType => "relay";

        /// <inheritdoc/>
        public async Task<Dictionary<string, Dictionary<string, object>>> GetRelayInfoAsync(string hubId, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            return await BackplaneConnectorProvider.InvokeAsync<Dictionary<string, Dictionary<string, object>>>(nameof(GetRelayInfoAsync), new object[] { hubId }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task SendDataHubAsync(SendRelayDataHub dataChanged, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            await BackplaneConnectorProvider.SendAsync(nameof(SendDataHubAsync), new object[] { dataChanged }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task NotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            await BackplaneConnectorProvider.InvokeAsync<object>(nameof(NotifyParticipantChangedAsync), new object[] { dataChanged }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task NotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            await BackplaneConnectorProvider.InvokeAsync<object>(nameof(NotifyRelayHubChangedAsync), new object[] { dataChanged }, cancellationToken);
        }

        /// <inheritdoc/>
        public Task UpdateMetricsAsync((string ServiceId, string Stamp) serviceInfo, RelayServiceMetrics metrics, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DisposeDataChangesAsync(DataChanged[] dataChanges, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public bool HandleException(string methodName, Exception error) => false;

        public Task FireSendDataHubAsync(SendRelayDataHub dataChanged, CancellationToken cancellationToken)
        {
            return SendDataChangedAsync?.Invoke(dataChanged, cancellationToken);
        }

        public Task FireNotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken)
        {
            return ParticipantChangedAsync?.Invoke(dataChanged, cancellationToken);
        }

        public Task FireNotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken)
        {
            return RelayHubChanged?.Invoke(dataChanged, cancellationToken);
        }
    }
}
