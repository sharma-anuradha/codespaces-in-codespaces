// <copyright file="RelayBackplaneService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// The relay backplane service that will sync local/global relays.
    /// </summary>
    public class RelayBackplaneService : BackplaneService<IRelayBackplaneManager, IRelayBackplaneServiceNotification>
    {
        public RelayBackplaneService(
            IEnumerable<IRelayBackplaneServiceNotification> relayBackplaneServiceNotifications,
            ILogger<RelayBackplaneService> logger,
            IRelayBackplaneManager backplaneManager)
            : base(backplaneManager, relayBackplaneServiceNotifications, logger)
        {
            BackplaneManager.ParticipantChangedAsync += OnParticipantChangedAsync;
            BackplaneManager.RelayHubChangedAsync += OnRelayHubChangedAsync;
            BackplaneManager.SendDataChangedAsync += OnSendDataChangedAsync;
        }

        private RelayHubManager RelayHubManager { get; } = new RelayHubManager();

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
            using (Logger.BeginMethodScope(nameof(SendDataHubAsync)))
            {
                Logger.LogDebug($"hub id:{dataChanged.HubId} type:{dataChanged.Type}");
            }

            BackplaneManager.TrackDataChanged(dataChanged);

            await FireSendDataHubAsync(dataChanged, cancellationToken);
            await BackplaneManager.SendDataHubAsync(dataChanged, DisposeToken);
        }

        public async Task NotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(NotifyParticipantChangedAsync)))
            {
                Logger.LogDebug($"hub id:{dataChanged.HubId} participant:{dataChanged.ParticipantId} change:{dataChanged.ChangeType}");
            }

            if (RelayHubManager.NotifyParticipantChangedAsync(dataChanged, out var relayHubInfo))
            {
                await BackplaneManager.UpdateRelayHubInfo(dataChanged.HubId, relayHubInfo, cancellationToken);
            }

            BackplaneManager.TrackDataChanged(dataChanged);

            await FireNotifyParticipantChangedAsync(dataChanged, cancellationToken);
            await BackplaneManager.NotifyParticipantChangedAsync(dataChanged, DisposeToken);
        }

        public async Task NotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(NotifyRelayHubChangedAsync)))
            {
                Logger.LogDebug($"hub id:{dataChanged.HubId} change:{dataChanged.ChangeType}");
            }

            RelayHubManager.NotifyRelayHubChangedAsync(dataChanged);

            BackplaneManager.TrackDataChanged(dataChanged);

            await FireNotifyRelayHubChangedAsync(dataChanged, cancellationToken);
            await BackplaneManager.NotifyRelayHubChangedAsync(dataChanged, DisposeToken);
        }

        private async Task FireSendDataHubAsync(SendRelayDataHub dataChanged, CancellationToken cancellationToken)
        {
            Logger.LogMethodScope(
                LogLevel.Debug,
                $"hubId:{dataChanged.HubId} type:{dataChanged.Type} size:{dataChanged.Data.Length}",
                nameof(FireSendDataHubAsync));
            foreach (var notify in BackplaneServiceNotifications)
            {
                await notify.FireSendDataHubAsync(dataChanged, cancellationToken);
            }
        }

        private async Task FireNotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken)
        {
            Logger.LogMethodScope(
                LogLevel.Debug,
                $"hubId:{dataChanged.HubId} participantId:{dataChanged.ParticipantId} change:{dataChanged.ChangeType}",
                nameof(FireNotifyParticipantChangedAsync));
            foreach (var notify in BackplaneServiceNotifications)
            {
                await notify.FireNotifyParticipantChangedAsync(dataChanged, cancellationToken);
            }
        }

        private async Task FireNotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken)
        {
            Logger.LogMethodScope(
                LogLevel.Debug,
                $"hubId:{dataChanged.HubId} change:{dataChanged.ChangeType}",
                nameof(FireNotifyRelayHubChangedAsync));
            foreach (var notify in BackplaneServiceNotifications)
            {
                await notify.FireNotifyRelayHubChangedAsync(dataChanged, cancellationToken);
            }
        }

        private async Task OnSendDataChangedAsync(
            SendRelayDataHub dataChanged,
            CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(OnSendDataChangedAsync)))
            {
                Logger.LogDebug($"hub id:{dataChanged.HubId} type:{dataChanged.Type} size:{dataChanged.Data.Length}");
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
            using (Logger.BeginMethodScope(nameof(OnRelayHubChangedAsync)))
            {
                Logger.LogDebug($"hub id:{dataChanged.HubId} change type:{dataChanged.ChangeType} ");
            }

            if (RelayHubManager.ContainsHub(dataChanged.HubId))
            {
                RelayHubManager.NotifyRelayHubChangedAsync(dataChanged);
                await FireNotifyRelayHubChangedAsync(dataChanged, cancellationToken);
            }
        }

        private async Task OnParticipantChangedAsync(
            RelayParticipantChanged dataChanged,
            CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(OnParticipantChangedAsync)))
            {
                Logger.LogDebug($"hubid:{dataChanged.HubId} participant id:{dataChanged.ParticipantId} change type:{dataChanged.ChangeType} ");
            }

            if (RelayHubManager.NotifyParticipantChangedAsync(dataChanged, out var relayHubInfo))
            {
                await FireNotifyParticipantChangedAsync(dataChanged, cancellationToken);
            }
        }
    }
}
