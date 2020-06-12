// <copyright file="RelayService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// The non Hub Service class instance that manage all the relay hubs.
    /// </summary>
    public class RelayService : HubService<RelayServiceHub, HubServiceOptions>, IAsyncDisposable
    {
        private const string HubIdScope = "HubId";
        private const string ConnectionScope = "Connection";

        private ConcurrentDictionary<string, RelayHub> relayHubs = new ConcurrentDictionary<string, RelayHub>();
        private ConcurrentDictionary<string, ConcurrentHashSet<RelayHub>> connectionHubs = new ConcurrentDictionary<string, ConcurrentHashSet<RelayHub>>();

        public RelayService(
            HubServiceOptions options,
            IEnumerable<IHubContextHost> hubContextHosts,
            ILogger<RelayService> logger,
            IRelayBackplaneManager backplaneManager = null,
            IServiceCounters hubServiceCounters = null,
            IDataFormatProvider formatProvider = null)
            : base(options, hubContextHosts, logger, hubServiceCounters, formatProvider)
        {
            BackplaneManager = backplaneManager;

            if (backplaneManager != null)
            {
                backplaneManager.SendDataChangedAsync += OnSendDataChangedAsync;
                backplaneManager.ParticipantChangedAsync += OnNotifyParticipantChangedAsync;
                backplaneManager.RelayHubChangedAsync += OnRelayHubChangedAsync;
                backplaneManager.MetricsFactory = () => (new ServiceInfo(ServiceId, options.Stamp, nameof(RelayService)), GetMetrics());
            }
        }

        public IRelayBackplaneManager BackplaneManager { get; }

        private IEnumerable<IGroupManager> AllGroups => HubContextHosts.Select(hCtxt => hCtxt.Groups);

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            Logger.LogDebug($"DisposeAsync");
            foreach (var relayHub in this.relayHubs.Values.Where(r => !r.IsBackplaneHub))
            {
                Logger.LogDebug($"Remove hub id:{relayHub.Id}");
                await NotifyHubChangedAsync(relayHub.Id, RelayHubChangeType.Removed, default);
            }
        }

        public RelayServiceMetrics GetMetrics()
        {
            return new RelayServiceMetrics(relayHubs.Count);
        }

        public async Task<string> CreateHubAsync(string hubId, CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(RelayServiceScopes.MethodCreateHub))
            {
                Logger.LogDebug($"hubId:{hubId}");
            }

            if (string.IsNullOrEmpty(hubId))
            {
                hubId = Guid.NewGuid().ToString();
            }
            else if (this.relayHubs.ContainsKey(hubId))
            {
                throw new HubException($"Relay hub id:{hubId} already exist");
            }

            await GetOrCreateRelayHubAsync(hubId, cancellationToken, true);

            return hubId;
        }

        public async Task DeleteHubAsync(string hubId, CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(RelayServiceScopes.MethodDeleteHub))
            {
                Logger.LogDebug($"hubId:{hubId}");
            }

            if (this.relayHubs.TryRemove(hubId, out var relayHub))
            {
                await relayHub.NotifyHubDeletedAsync(cancellationToken);
            }

            // backplane support
            await NotifyHubChangedAsync(hubId, RelayHubChangeType.Removed, cancellationToken);
        }

        public async Task<Dictionary<string, Dictionary<string, object>>> JoinHubAsync(
            string connectionId,
            string hubId,
            Dictionary<string, object> properties,
            JoinOptions joinOptions,
            CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNullOrEmpty(hubId, nameof(hubId));

            using (BeginHubScope(RelayServiceScopes.MethodJoinHub, hubId, connectionId))
            {
                Logger.LogDebug($"properties:{properties.ConvertToString(FormatProvider)}");
            }

            var relayHub = await GetOrCreateRelayHubAsync(hubId, cancellationToken, joinOptions.CreateIfNotExists);
            this.connectionHubs.AddOrUpdate(connectionId, (hubs) => hubs.Add(relayHub));
            await Task.WhenAll(AllGroups.Select(g => g.AddToGroupAsync(connectionId, relayHub.GroupName, cancellationToken)));
            var result = await relayHub.JoinAsync(connectionId, properties, cancellationToken);

            // backplane support
            await NotifyParticipantChangedAsync(hubId, connectionId, result.participants[connectionId], result.isNewParticipant ? ParticipantChangeType.Added : ParticipantChangeType.Updated, cancellationToken);

            return result.participants;
        }

        public async Task LeaveHubAsync(string connectionId, string hubId, CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNullOrEmpty(hubId, nameof(hubId));

            using (BeginHubScope(RelayServiceScopes.MethodLeaveHub, hubId, connectionId))
            {
                Logger.LogDebug("Leaving from API");
            }

            var relayHub = await GetOrCreateRelayHubAsync(hubId, cancellationToken);
            this.connectionHubs.AddOrUpdate(connectionId, (hubs) => hubs.TryRemove(relayHub));

            await LeaveHubAsync(relayHub, connectionId, cancellationToken);
        }

        public async Task<int> SendDataHubAsync(
            string connectionId,
            string hubId,
            SendOption sendOption,
            string[] targetParticipantIds,
            string type,
            byte[] data,
            Dictionary<string, object> messageProperties,
            CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNullOrEmpty(hubId, nameof(hubId));
            Requires.NotNullOrEmpty(type, nameof(type));

            var relayHub = await GetOrCreateRelayHubAsync(hubId, cancellationToken);

            // extract audit properties
            Dictionary<string, object> auditProperties = null;
            if (messageProperties != null)
            {
                auditProperties = messageProperties
                    .Where(kvp => kvp.Key.StartsWith(RelayHubMessageProperties.PropertyAuditPrefixId))
                    .ToDictionary(kvp => kvp.Key.Substring(RelayHubMessageProperties.PropertyAuditPrefixId.Length), kvp => kvp.Value);
                if (auditProperties.Count > 0)
                {
                    messageProperties = messageProperties
                        .Where(kvp => !kvp.Key.StartsWith(RelayHubMessageProperties.PropertyAuditPrefixId))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    // track a perf type for this hub message
                    if (auditProperties.TryGetValue(RelayHubMessageProperties.PropertyAuditPerfTypeId, out var perfType))
                    {
                        MethodPerf($"{nameof(SendDataHubAsync)}_{perfType}", TimeSpan.Zero);
                    }
                }
            }

            var uniqueId = await relayHub.SendDataAsync(connectionId, null, sendOption, targetParticipantIds, type, data, messageProperties, cancellationToken);
            using (BeginHubScope(RelayServiceScopes.MethodSendDataHub, hubId, connectionId))
            {
                var targetParticipantIdsStr = targetParticipantIds != null ? string.Join(",", targetParticipantIds) : "*";
                Logger.LogDebug($"uniqueId:{uniqueId} sendOption:{sendOption} targetParticipantIds:{targetParticipantIdsStr} type:{type} data-length:{data?.Length} messageProperties:{messageProperties?.ConvertToString(FormatProvider)} auditProperties:{auditProperties.ConvertToString(FormatProvider)}");
            }

            if (BackplaneManager != null)
            {
                var relayDataHub = new SendRelayDataHub(
                    Guid.NewGuid().ToString(),
                    ServiceId,
                    hubId,
                    uniqueId,
                    sendOption,
                    connectionId,
                    targetParticipantIds,
                    type,
                    data,
                    messageProperties);
                await BackplaneManager.SendDataHubAsync(relayDataHub, cancellationToken);
            }

            // Note: we want to track perf numbers by
            MethodPerf($"{nameof(SendDataHubAsync)}_{type}", sw.Elapsed);

            return uniqueId;
        }

        public async Task UpdateAsync(string connectionId, string hubId, Dictionary<string, object> properties, CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNullOrEmpty(hubId, nameof(hubId));

            using (BeginHubScope(RelayServiceScopes.MethodUpdateHub, hubId, connectionId))
            {
                Logger.LogDebug($"properties:{properties.ConvertToString(FormatProvider)}");
            }

            var relayHub = await GetOrCreateRelayHubAsync(hubId, cancellationToken);
            await relayHub.UpdateAsync(connectionId, properties, cancellationToken);

            // backplane support
            await NotifyParticipantChangedAsync(hubId, connectionId, properties, ParticipantChangeType.Updated, cancellationToken);
        }

        public async Task DisconnectAsync(string connectionId, CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));

            if (this.connectionHubs.TryRemove(connectionId, out var relayHubs))
            {
                await Task.WhenAll(relayHubs.Values.Select(async hub =>
                {
                    using (BeginHubScope(RelayServiceScopes.MethodDisconnectHub, hub.Id, connectionId))
                    {
                        Logger.LogDebug("Leaving hub from disconnect");
                    }

                    await LeaveHubAsync(hub, connectionId, cancellationToken);
                }));
            }
        }

        private IDisposable BeginHubScope(string method, string hubId, string connectionId)
        {
            return Logger.BeginScope(
                    (LoggerScopeHelpers.MethodScope, method),
                    (HubIdScope, hubId),
                    (ConnectionScope, connectionId));
        }

        private IDisposable BeginHubScope(string method, string hubId)
        {
            return Logger.BeginScope(
                    (LoggerScopeHelpers.MethodScope, method),
                    (HubIdScope, hubId));
        }

        private async Task LeaveHubAsync(RelayHub relayHub, string participantId, CancellationToken cancellationToken)
        {
            await relayHub.LeaveAsync(participantId, cancellationToken);
            await Task.WhenAll(AllGroups.Select(g => g.RemoveFromGroupAsync(participantId, relayHub.GroupName, cancellationToken)));

            // backplane support
            await NotifyParticipantChangedAsync(relayHub.Id, participantId, null, ParticipantChangeType.Removed, cancellationToken);
        }

        private async Task NotifyParticipantChangedAsync(
            string hubId,
            string participantId,
            Dictionary<string, object> properties,
            ParticipantChangeType changeType,
            CancellationToken cancellationToken)
        {
            if (BackplaneManager != null)
            {
                var participantChanged = new RelayParticipantChanged(
                    Guid.NewGuid().ToString(),
                    ServiceId,
                    hubId,
                    participantId,
                    properties,
                    changeType);
                await BackplaneManager.NotifyParticipantChangedAsync(participantChanged, cancellationToken);
            }
        }

        private async Task NotifyHubChangedAsync(
            string hubId,
            RelayHubChangeType changeType,
            CancellationToken cancellationToken)
        {
            if (BackplaneManager != null)
            {
                var relayHubChanged = new RelayHubChanged(
                    Guid.NewGuid().ToString(),
                    ServiceId,
                    hubId,
                    changeType);
                await BackplaneManager.NotifyRelayHubChangedAsync(relayHubChanged, cancellationToken);
            }
        }

        private async Task<RelayHub> GetOrCreateRelayHubAsync(string hubId, CancellationToken cancellationToken, bool createIfNotExists = false)
        {
            if (createIfNotExists)
            {
                bool isCreated = false;
                var relayHub = this.relayHubs.GetOrAdd(hubId, (id) =>
                {
                    isCreated = true;
                    return new RelayHub(this, id, false);
                });

                if (isCreated)
                {
                    // backplane support to sync with 'other' participants
                    if (BackplaneManager != null)
                    {
                        var relayInfo = await BackplaneManager.GetRelayInfoAsync(hubId, cancellationToken);
                        if (relayInfo != null)
                        {
                            relayHub.SetOtherParticipants(relayInfo);
                        }
                    }

                    using (BeginHubScope(nameof(GetOrCreateRelayHubAsync), hubId))
                    {
                        Logger.LogDebug($"Hub created");
                    }

                    await NotifyHubChangedAsync(hubId, RelayHubChangeType.Created, cancellationToken);
                }

                return relayHub;
            }
            else
            {
                if (this.relayHubs.TryGetValue(hubId, out var relayHub))
                {
                    return relayHub;
                }

                // if we don't locally host yet the hub we will need to look on our backplane
                // if there is an existing hub with the same id.
                if (BackplaneManager != null)
                {
                    var relayInfo = await BackplaneManager.GetRelayInfoAsync(hubId, cancellationToken);
                    if (relayInfo != null)
                    {
                        return this.relayHubs.GetOrAdd(hubId, (id) =>
                        {
                            var relayHub = new RelayHub(this, id, true);
                            relayHub.SetOtherParticipants(relayInfo);
                            using (BeginHubScope(nameof(GetOrCreateRelayHubAsync), hubId))
                            {
                                Logger.LogDebug($"Hub created from backplane");
                            }

                            return relayHub;
                        });
                    }
                }

                throw new HubException($"No relay hub found for:{hubId}");
            }
        }

        private async Task OnSendDataChangedAsync(
            SendRelayDataHub dataChanged,
            CancellationToken cancellationToken)
        {
            // ignore self notifications
            if (dataChanged.ServiceId == ServiceId)
            {
                return;
            }

            var sw = Stopwatch.StartNew();
            if (this.relayHubs.TryGetValue(dataChanged.HubId, out var relayHub))
            {
                using (BeginHubScope(nameof(OnSendDataChangedAsync), dataChanged.HubId))
                {
                    Logger.LogDebug($"serviceId:{dataChanged.ServiceId} uniqueId:{dataChanged.UniqueId} sendOption:{dataChanged.SendOption} type:{dataChanged.Type} data-length:{dataChanged.Data?.Length}");
                }

                await relayHub.SendDataAsync(
                    dataChanged.FromParticipantId,
                    dataChanged.UniqueId,
                    SendOption.None,
                    dataChanged.TargetParticipantIds,
                    dataChanged.Type,
                    dataChanged.Data,
                    dataChanged.MessageProperties,
                    cancellationToken);
            }

            MethodPerf(nameof(OnSendDataChangedAsync), sw.Elapsed);
        }

        private async Task OnNotifyParticipantChangedAsync(
            RelayParticipantChanged dataChanged,
            CancellationToken cancellationToken)
        {
            // ignore self notifications
            if (dataChanged.ServiceId == ServiceId)
            {
                return;
            }

            var sw = Stopwatch.StartNew();
            if (this.relayHubs.TryGetValue(dataChanged.HubId, out var relayHub))
            {
                using (BeginHubScope(nameof(OnNotifyParticipantChangedAsync), dataChanged.HubId))
                {
                    Logger.LogDebug($"serviceId:{dataChanged.ServiceId} participantId:{dataChanged.ParticipantId} change type:{dataChanged.ChangeType}");
                }

                await relayHub.NotifyParticipantChangedAsync(dataChanged, cancellationToken);
            }

            MethodPerf(nameof(OnNotifyParticipantChangedAsync), sw.Elapsed);
        }

        private async Task OnRelayHubChangedAsync(
            RelayHubChanged dataChanged,
            CancellationToken cancellationToken)
        {
            // ignore self notifications
            if (dataChanged.ServiceId == ServiceId)
            {
                return;
            }

            if (dataChanged.ChangeType == RelayHubChangeType.Removed && this.relayHubs.TryRemove(dataChanged.HubId, out var relayHub))
            {
                using (BeginHubScope(nameof(OnRelayHubChangedAsync), dataChanged.HubId))
                {
                    Logger.LogDebug($"serviceId:{dataChanged.ServiceId} -> Hub delete from backplane");
                }

                await relayHub.NotifyHubDeletedAsync(cancellationToken);
            }
        }
    }
}
