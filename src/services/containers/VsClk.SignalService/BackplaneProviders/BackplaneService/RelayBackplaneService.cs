// <copyright file="RelayBackplaneService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// The relay backplane service that will sync local/global relays.
    /// </summary>
    public class RelayBackplaneService : BackplaneService<IRelayBackplaneManager, IRelayBackplaneServiceNotification>, IHostedService
    {
        private const int MaxSendDataHubBuffer = 5000;

        private const string HubIdScope = "HubId";

        public RelayBackplaneService(
            IEnumerable<IRelayBackplaneServiceNotification> relayBackplaneServiceNotifications,
            ILogger<RelayBackplaneService> logger,
            IRelayBackplaneManager backplaneManager)
            : base(backplaneManager, relayBackplaneServiceNotifications, logger)
        {
            SendDataHubActionBlock = CreateActionBlock<SendRelayDataHub>(
                nameof(SendDataHubActionBlock),
                async (elapsed, dataChanged) =>
                {
                    try
                    {
                        await BackplaneManager.SendDataHubAsync(dataChanged, DisposeToken);
                    }
                    finally
                    {
                        TrackDataChanged(dataChanged, TrackDataChangedOptions.Refresh);
                    }
                },
                item => item.ChangeId,
                1,
                MaxSendDataHubBuffer);
            BackplaneManager.ParticipantChangedAsync += OnParticipantChangedAsync;
            BackplaneManager.RelayHubChangedAsync += OnRelayHubChangedAsync;
            BackplaneManager.SendDataChangedAsync += OnSendDataChangedAsync;
        }

        private ActionBlock<(Stopwatch, SendRelayDataHub)> SendDataHubActionBlock { get; }

        private RelayHubManager RelayHubManager { get; } = new RelayHubManager();

        /// <inheritdoc/>
        public Task RunAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task DisposeAsync()
        {
            await CompleteActionBlockAsync(SendDataHubActionBlock, nameof(SendDataHubActionBlock));
        }

        public async Task<Dictionary<string, Dictionary<string, object>>> GetRelayInfoAsync(string hubId, CancellationToken cancellationToken)
        {
            if (RelayHubManager.TryGetRelayInfo(hubId, out var relayInfo))
            {
                return relayInfo;
            }

            var backplaneRelayInfo = await BackplaneManager.GetRelayInfoAsync(hubId, cancellationToken);
            return backplaneRelayInfo;
        }

        public async Task SendDataHubAsync(SendRelayDataHub dataChanged, CancellationToken cancellationToken)
        {
            using (BeginHubScope(dataChanged.HubId, nameof(SendDataHubAsync)))
            {
                Log(dataChanged);
            }

            TrackDataChanged(dataChanged, TrackDataChangedOptions.Lock);

            await FireSendDataHubAsync(dataChanged, cancellationToken);
            await SendDataHubActionBlock.SendAsync((Stopwatch.StartNew(), dataChanged));
        }

        public async Task NotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken)
        {
            using (BeginHubScope(dataChanged.HubId, nameof(NotifyParticipantChangedAsync)))
            {
                Log(dataChanged);
            }

            if (RelayHubManager.NotifyParticipantChanged(dataChanged, out var relayHubInfo))
            {
                await BackplaneManager.UpdateRelayHubInfo(dataChanged.HubId, relayHubInfo, cancellationToken);
            }

            TrackDataChanged(dataChanged);

            await FireNotifyParticipantChangedAsync(dataChanged, cancellationToken);
            await BackplaneManager.NotifyParticipantChangedAsync(dataChanged, DisposeToken);
        }

        public async Task NotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken)
        {
            using (BeginHubScope(dataChanged.HubId, nameof(NotifyRelayHubChangedAsync)))
            {
                Log(dataChanged);
            }

            RelayHubManager.NotifyRelayHubChanged(dataChanged);

            TrackDataChanged(dataChanged);

            await FireNotifyRelayHubChangedAsync(dataChanged, cancellationToken);
            await BackplaneManager.NotifyRelayHubChangedAsync(dataChanged, DisposeToken);
        }

        private IDisposable BeginHubScope(string hubId, string method)
        {
            return Logger.BeginScope(
                    (LoggerScopeHelpers.MethodScope, method),
                    (HubIdScope, hubId));
        }

        private void Log(SendRelayDataHub dataChanged, string prefix = null)
        {
            Logger.LogDebug($"{prefix ?? string.Empty}uniqueId:{dataChanged.UniqueId} type:{dataChanged.Type} from:{dataChanged.FromParticipantId} length:{dataChanged.Data?.Length}");
        }

        private void Log(RelayParticipantChanged dataChanged, string prefix = null)
        {
            Logger.LogDebug($"{prefix ?? string.Empty}participant:{dataChanged.ParticipantId} change:{dataChanged.ChangeType}");
        }

        private void Log(RelayHubChanged dataChanged, string prefix = null)
        {
            Logger.LogDebug($"{prefix ?? string.Empty}change:{dataChanged.ChangeType}");
        }

        private async Task FireSendDataHubAsync(SendRelayDataHub dataChanged, CancellationToken cancellationToken)
        {
            foreach (var notify in BackplaneServiceNotifications)
            {
                await notify.FireSendDataHubAsync(dataChanged, cancellationToken);
            }
        }

        private async Task FireNotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken)
        {
            foreach (var notify in BackplaneServiceNotifications)
            {
                await notify.FireNotifyParticipantChangedAsync(dataChanged, cancellationToken);
            }
        }

        private async Task FireNotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken)
        {
            foreach (var notify in BackplaneServiceNotifications)
            {
                await notify.FireNotifyRelayHubChangedAsync(dataChanged, cancellationToken);
            }
        }

        private async Task OnSendDataChangedAsync(
            SendRelayDataHub dataChanged,
            CancellationToken cancellationToken)
        {
            using (BeginHubScope(dataChanged.HubId, nameof(OnSendDataChangedAsync)))
            {
                Log(dataChanged, $"serviceId:{dataChanged.ServiceId}->");
            }

            if (RelayHubManager.ContainsHub(dataChanged.HubId))
            {
                await FireSendDataHubAsync(dataChanged, cancellationToken);
            }
        }

        private async Task OnRelayHubChangedAsync(
            RelayHubChanged dataChanged,
            CancellationToken cancellationToken)
        {
            using (BeginHubScope(dataChanged.HubId, nameof(OnRelayHubChangedAsync)))
            {
                Log(dataChanged, $"serviceId:{dataChanged.ServiceId}->");
            }

            if (RelayHubManager.ContainsHub(dataChanged.HubId))
            {
                RelayHubManager.NotifyRelayHubChanged(dataChanged);
                await FireNotifyRelayHubChangedAsync(dataChanged, cancellationToken);
            }
        }

        private async Task OnParticipantChangedAsync(
            RelayParticipantChanged dataChanged,
            CancellationToken cancellationToken)
        {
            using (BeginHubScope(dataChanged.HubId, nameof(OnParticipantChangedAsync)))
            {
                Log(dataChanged, $"serviceId:{dataChanged.ServiceId}->");
            }

            if (RelayHubManager.NotifyParticipantChanged(dataChanged, out var relayHubInfo))
            {
                await FireNotifyParticipantChangedAsync(dataChanged, cancellationToken);
            }
        }
    }
}
