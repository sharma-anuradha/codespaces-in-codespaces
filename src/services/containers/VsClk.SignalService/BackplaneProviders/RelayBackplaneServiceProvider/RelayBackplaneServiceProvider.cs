// <copyright file="RelayBackplaneServiceProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    public class RelayBackplaneServiceProvider : BackplaneServiceProviderBase, IRelayBackplaneProvider
    {
        public RelayBackplaneServiceProvider(
            IBackplaneConnectorProvider backplaneConnectorProvider,
            string hostServiceId,
            ILogger logger,
            CancellationToken stoppingToken)
            : base(backplaneConnectorProvider, hostServiceId, logger, stoppingToken)
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
            await BackplaneConnectorProvider.InvokeAsync<object>(nameof(SendDataHubAsync), new object[] { dataChanged }, cancellationToken);
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
        public Task UpdateMetricsAsync(ServiceInfo serviceInfo, RelayServiceMetrics metrics, CancellationToken cancellationToken)
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

        public async Task FireSendDataHubAsync(SendRelayDataHub dataChanged, CancellationToken cancellationToken)
        {
            try
            {
                if (SendDataChangedAsync != null)
                {
                    await SendDataChangedAsync.Invoke(dataChanged, cancellationToken);
                }
            }
            catch (Exception err)
            {
                Logger.LogMethodScope(
                    LogLevel.Error,
                    err,
                    $"Failed to handle hub data changeId:{dataChanged.ChangeId} hubId:{dataChanged.HubId} id:{dataChanged.UniqueId} type:{dataChanged.Type}",
                    nameof(FireSendDataHubAsync));
            }
        }

        public async Task FireNotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken)
        {
            try
            {
                if (ParticipantChangedAsync != null)
                {
                    await ParticipantChangedAsync.Invoke(dataChanged, cancellationToken);
                }
            }
            catch (Exception err)
            {
                Logger.LogMethodScope(
                    LogLevel.Error,
                    err,
                    $"Failed to handle participant data changeId:{dataChanged.ChangeId} hubId:{dataChanged.HubId} participantId:{dataChanged.ParticipantId} changeType:{dataChanged.ChangeType}",
                    nameof(FireNotifyParticipantChangedAsync));
            }
        }

        public async Task FireNotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken)
        {
            try
            {
                if (RelayHubChanged != null)
                {
                    await RelayHubChanged.Invoke(dataChanged, cancellationToken);
                }
            }
            catch (Exception err)
            {
                Logger.LogMethodScope(
                    LogLevel.Error,
                    err,
                    $"Failed to handle hub changed changeId:{dataChanged.ChangeId} hubId:{dataChanged.HubId} changeType:{dataChanged.ChangeType}",
                    nameof(FireNotifyParticipantChangedAsync));
            }
        }
    }
}
