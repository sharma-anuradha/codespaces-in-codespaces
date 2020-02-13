// <copyright file="RelayBackplaneManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.Services.Backplane.Common;
using Microsoft.VsCloudKernel.SignalService.Common;
using RelayHubInfo = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// A backplane manager that can host multiple backplane providers.
    /// </summary>
    public class RelayBackplaneManager : BackplaneManagerBase<IRelayBackplaneProvider, RelayBackplaneProviderSupportLevel, RelayServiceMetrics>,  IRelayBackplaneManager
    {
        public RelayBackplaneManager(ILogger<RelayBackplaneManager> logger, IDataFormatProvider formatProvider = null)
            : base(logger, formatProvider)
        {
        }

        /// <inheritdoc/>
        public event OnRelayDataChangedAsync<RelayParticipantChanged> ParticipantChangedAsync;

        /// <inheritdoc/>
        public event OnRelayDataChangedAsync<SendRelayDataHub> SendDataChangedAsync;

        /// <inheritdoc/>
        public event OnRelayDataChangedAsync<RelayHubChanged> RelayHubChangedAsync;

        /// <inheritdoc/>
        public async Task<RelayHubInfo> GetRelayInfoAsync(string hubId, CancellationToken cancellationToken)
        {
            return await WaitFirstOrDefault(
                GetSupportedProviders(s => BackplaneProviderSupportLevelConst.DefaultSupportThreshold).Select(p => (p.GetRelayInfoAsync(hubId, cancellationToken), p)),
                nameof(IRelayBackplaneProvider.GetRelayInfoAsync),
                (relayInfo) => $"size:{relayInfo?.Count}",
                r => r != null);
        }

        /// <inheritdoc/>
        public async Task SendDataHubAsync(SendRelayDataHub dataChanged, CancellationToken cancellationToken)
        {
            await DisposeExpiredDataChangesAsync(100, CancellationToken.None);
            await WaitAll(
                GetSupportedProviders(s => BackplaneProviderSupportLevelConst.DefaultSupportThreshold).Select(p => (p.SendDataHubAsync(dataChanged, cancellationToken) as Task, p)),
                nameof(IRelayBackplaneProvider.SendDataHubAsync),
                $"hubId:{dataChanged.HubId}");
        }

        /// <inheritdoc/>
        public async Task NotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken)
        {
            await DisposeExpiredDataChangesAsync(100, cancellationToken);
            await WaitAll(
                GetSupportedProviders(s => BackplaneProviderSupportLevelConst.DefaultSupportThreshold).Select(p => (p.NotifyParticipantChangedAsync(dataChanged, cancellationToken), p)),
                nameof(IRelayBackplaneProvider.NotifyParticipantChangedAsync),
                $"hubId:{dataChanged.HubId}");
        }

        /// <inheritdoc/>
        public async Task NotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken)
        {
            await DisposeExpiredDataChangesAsync(100, cancellationToken);
            await WaitAll(
                GetSupportedProviders(s => BackplaneProviderSupportLevelConst.DefaultSupportThreshold).Select(p => (p.NotifyRelayHubChangedAsync(dataChanged, cancellationToken), p)),
                nameof(IRelayBackplaneProvider.NotifyRelayHubChangedAsync),
                $"hubId:{dataChanged.HubId}");
        }

        /// <inheritdoc/>
        public async Task UpdateRelayHubInfo(string hubId, RelayHubInfo relayHubInfo, CancellationToken cancellationToken)
        {
            await WaitAll(
                BackplaneProviders.OfType<IRelayBackplaneManagerProvider>().Select(p => (p.UpdateRelayHubInfo(hubId, relayHubInfo, cancellationToken), p as IRelayBackplaneProvider)),
                nameof(IRelayBackplaneManagerProvider.UpdateRelayHubInfo),
                $"hubId:{hubId}");
        }

        /// <inheritdoc/>
        protected override void OnRegisterProvider(IRelayBackplaneProvider backplaneProvider)
        {
            backplaneProvider.ParticipantChangedAsync = (relayDataChanged, ct) => OnParticipantChangedAsync(backplaneProvider, relayDataChanged, ct);
            backplaneProvider.SendDataChangedAsync = (relayDataChanged, ct) => OnSendDataChangedAsync(backplaneProvider, relayDataChanged, ct);
            backplaneProvider.RelayHubChanged = (relayDataChanged, ct) => OnRelayHubChangedAsync(backplaneProvider, relayDataChanged, ct);
        }

        /// <inheritdoc/>
        protected override void AddMetricsScope(List<(string, object)> metricsScope, RelayServiceMetrics metrics)
        {
        }

        private async Task OnParticipantChangedAsync(
            IRelayBackplaneProvider backplaneProvider,
            RelayParticipantChanged relayParticipantChanged,
            CancellationToken cancellationToken)
        {
            if (TrackDataChanged(relayParticipantChanged))
            {
                return;
            }

            var stopWatch = Stopwatch.StartNew();
            if (ParticipantChangedAsync != null)
            {
                await ParticipantChangedAsync.Invoke(relayParticipantChanged, cancellationToken);
            }

            Logger.LogScope(
                LogLevel.Debug,
                $"provider:{backplaneProvider.GetType().Name} changed Id:{relayParticipantChanged.ChangeId}",
                (LoggerScopeHelpers.MethodScope, nameof(OnParticipantChangedAsync)),
                (LoggerScopeHelpers.MethodPerfScope, stopWatch.ElapsedMilliseconds));
        }

        private async Task OnSendDataChangedAsync(
            IRelayBackplaneProvider backplaneProvider,
            SendRelayDataHub sendRelayDataHub,
            CancellationToken cancellationToken)
        {
            if (TrackDataChanged(sendRelayDataHub))
            {
                return;
            }

            var stopWatch = Stopwatch.StartNew();
            if (SendDataChangedAsync != null)
            {
                await SendDataChangedAsync.Invoke(sendRelayDataHub, cancellationToken);
            }

            Logger.LogScope(
                LogLevel.Debug,
                $"provider:{backplaneProvider.GetType().Name} changed Id:{sendRelayDataHub.ChangeId}",
                (LoggerScopeHelpers.MethodScope, nameof(OnSendDataChangedAsync)),
                (LoggerScopeHelpers.MethodPerfScope, stopWatch.ElapsedMilliseconds));
        }

        private async Task OnRelayHubChangedAsync(
            IRelayBackplaneProvider backplaneProvider,
            RelayHubChanged relayHubChanged,
            CancellationToken cancellationToken)
        {
            if (TrackDataChanged(relayHubChanged))
            {
                return;
            }

            var stopWatch = Stopwatch.StartNew();
            if (RelayHubChangedAsync != null)
            {
                await RelayHubChangedAsync.Invoke(relayHubChanged, cancellationToken);
            }

            Logger.LogScope(
                LogLevel.Debug,
                $"provider:{backplaneProvider.GetType().Name} changed Id:{relayHubChanged.ChangeId}",
                (LoggerScopeHelpers.MethodScope, nameof(OnRelayHubChangedAsync)),
                (LoggerScopeHelpers.MethodPerfScope, stopWatch.ElapsedMilliseconds));
        }
    }
}
