// <copyright file="RelayService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public class RelayService : HubService<RelayServiceHub, HubServiceOptions>
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
            IDataFormatProvider formatProvider = null)
            : base(options, hubContextHosts, logger, formatProvider)
        {
            BackplaneManager = backplaneManager;

            if (backplaneManager != null)
            {
                backplaneManager.SendDataChangedAsync += OnSendDataChangedAsync;
                backplaneManager.ParticipantChangedAsync += OnNotifyParticipantChangedAsync;
                backplaneManager.RelayHubChangedAsync += OnRelayHubChangedAsync;
                backplaneManager.MetricsFactory = () => ((ServiceId, options.Stamp), GetMetrics());
            }
        }

        public IRelayBackplaneManager BackplaneManager { get; }

        private IEnumerable<IGroupManager> AllGroups => HubContextHosts.Select(hCtxt => hCtxt.Groups);

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

            await GetOrCreateRelayHubAsync(hubId, true);

            return hubId;
        }

        public async Task DeleteHubAsync(string hubId, CancellationToken cancellationToken)
        {
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

            var relayHub = await GetOrCreateRelayHubAsync(hubId, joinOptions.CreateIfNotExists);
            this.connectionHubs.AddOrUpdate(connectionId, (hubs) => hubs.Add(relayHub));
            await Task.WhenAll(AllGroups.Select(g => g.AddToGroupAsync(connectionId, relayHub.GroupName, cancellationToken)));
            var result = await relayHub.JoinAsync(connectionId, properties, cancellationToken);

            // backplane support
            await NotifyParticipantChangedAsync(hubId, connectionId, properties, ParticipantChangeType.Added, cancellationToken);

            return result;
        }

        public async Task LeaveHubAsync(string connectionId, string hubId, CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNullOrEmpty(hubId, nameof(hubId));

            using (BeginHubScope(RelayServiceScopes.MethodLeaveHub, hubId, connectionId))
            {
                Logger.LogDebug("Leaving from API");
            }

            var relayHub = await GetOrCreateRelayHubAsync(hubId);
            this.connectionHubs.AddOrUpdate(connectionId, (hubs) => hubs.TryRemove(relayHub));

            await LeaveHubAsync(relayHub, connectionId, cancellationToken);
        }

        public async Task SendDataHubAsync(
            string connectionId,
            string hubId,
            SendOption sendOption,
            string[] targetParticipantIds,
            string type,
            byte[] data,
            CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNullOrEmpty(hubId, nameof(hubId));
            Requires.NotNullOrEmpty(type, nameof(type));
            Requires.NotNull(data, nameof(data));

            using (BeginHubScope(RelayServiceScopes.MethodSendDataHub, hubId, connectionId))
            {
                var targetParticipantIdsStr = targetParticipantIds != null ? string.Join(",", targetParticipantIds) : "*";
                Logger.LogDebug($"sendOption:{sendOption} targetParticipantIds:{targetParticipantIdsStr} type:{type} data-length:{data?.Length}");
            }

            var relayHub = await GetOrCreateRelayHubAsync(hubId);
            await relayHub.SendDataAsync(connectionId, sendOption, targetParticipantIds, type, data, cancellationToken);

            if (BackplaneManager != null)
            {
                var relayDataHub = new SendRelayDataHub(
                    Guid.NewGuid().ToString(),
                    ServiceId,
                    hubId,
                    sendOption,
                    connectionId,
                    targetParticipantIds,
                    type,
                    data);
                await BackplaneManager.SendDataHubAsync(relayDataHub, cancellationToken);
            }
        }

        public async Task UpdateAsync(string connectionId, string hubId, Dictionary<string, object> properties, CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNullOrEmpty(hubId, nameof(hubId));

            using (BeginHubScope(RelayServiceScopes.MethodUpdateHub, hubId, connectionId))
            {
                Logger.LogDebug($"properties:{properties.ConvertToString(FormatProvider)}");
            }

            var relayHub = await GetOrCreateRelayHubAsync(hubId);
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

        private async Task<RelayHub> GetOrCreateRelayHubAsync(string hubId, bool createIfNotExists = false)
        {
            if (createIfNotExists)
            {
                bool isCreated = false;
                var relayHub = this.relayHubs.GetOrAdd(hubId, (id) =>
                {
                    isCreated = true;
                    return new RelayHub(this, id);
                });

                if (isCreated)
                {
                    // backplane support to sync with 'other' participants
                    if (BackplaneManager != null)
                    {
                        var relayInfo = await BackplaneManager.GetRelayInfoAsync(hubId, default);
                        if (relayInfo != null)
                        {
                            relayHub.SetOtherParticipants(relayInfo);
                        }
                    }

                    Logger.LogDebug($"Hub created with id:{hubId}");
                    await NotifyHubChangedAsync(hubId, RelayHubChangeType.Created, default);
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
                    var relayInfo = await BackplaneManager.GetRelayInfoAsync(hubId, default);
                    if (relayInfo != null)
                    {
                        return this.relayHubs.GetOrAdd(hubId, (id) =>
                        {
                            var relayHub = new RelayHub(this, id);
                            relayHub.SetOtherParticipants(relayInfo);
                            Logger.LogDebug($"Hub created from backplane id:{hubId}");
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

            if (this.relayHubs.TryGetValue(dataChanged.HubId, out var relayHub))
            {
                await relayHub.SendDataAsync(
                    dataChanged.FromParticipantId,
                    SendOption.None,
                    dataChanged.TargetParticipantIds,
                    dataChanged.Type,
                    dataChanged.Data,
                    cancellationToken);
            }
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

            if (this.relayHubs.TryGetValue(dataChanged.HubId, out var relayHub))
            {
                await relayHub.NotifyParticipantChangedAsync(dataChanged, cancellationToken);
            }
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
                await relayHub.NotifyHubDeletedAsync(cancellationToken);
            }
        }
    }
}
