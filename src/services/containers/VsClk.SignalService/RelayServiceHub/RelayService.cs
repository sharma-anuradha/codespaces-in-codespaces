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
    /// The non Hub Service class instance that manage all the relay hubs
    /// </summary>
    public class RelayService : HubService<RelayServiceHub>
    {
        private const string HubIdScope = "HubId";
        private const string ConnectionScope = "Connection";

        private ConcurrentDictionary<string, RelayHub> relayHubs = new ConcurrentDictionary<string, RelayHub>();
        private ConcurrentDictionary<string, ConcurrentHashSet<RelayHub>> connectionHubs = new ConcurrentDictionary<string, ConcurrentHashSet<RelayHub>>();

        public RelayService(
            RelayServiceOptions options,
            IEnumerable<IHubContextHost> hubContextHosts,
            ILogger<RelayService> logger,
            IHubFormatProvider formatProvider = null)
            : base(options.Id, hubContextHosts, logger, formatProvider)
        {
        }

        private IEnumerable<IGroupManager> AllGroups => HubContextHosts.Select(hCtxt => hCtxt.Groups);

        public RelayServiceMetrics GetMetrics()
        {
            return new RelayServiceMetrics(relayHubs.Count);
        }

        public Task<string> CreateHubAsync(string hubId, CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(RelayServiceScopes.MethodCreateHub))
            {
                Logger.LogDebug($"hubId:{hubId}");
            }

            if (string.IsNullOrEmpty(hubId))
            {
                hubId = Guid.NewGuid().ToString();
            }
            else if(this.relayHubs.ContainsKey(hubId))
            {
                throw new HubException($"Relay hub id:{hubId} already exist");
            }

            var relayHub = new RelayHub(this, hubId);
            this.relayHubs.TryAdd(hubId, relayHub);

            Logger.LogDebug($"Hub created with id:{hubId}");

            return Task.FromResult(hubId);
        }

        public async Task<Dictionary<string, Dictionary<string, object>>> JoinHubAsync(string connectionId, string hubId, Dictionary<string, object> properties, bool createIfNotExists, CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNullOrEmpty(hubId, nameof(hubId));

            using (BeginHubScope(RelayServiceScopes.MethodJoinHub, hubId, connectionId))
            {
                Logger.LogDebug($"properties:{properties.ConvertToString(FormatProvider)}");
            }

            var relayHub = GetRelayHub(hubId, createIfNotExists);
            this.connectionHubs.AddOrUpdate(connectionId, (hubs) => hubs.Add(relayHub));
            await Task.WhenAll(AllGroups.Select(g => g.AddToGroupAsync(connectionId, hubId, cancellationToken)));
            return await relayHub.JoinAsync(connectionId, properties, cancellationToken);
        }

        public async Task LeaveHubAsync(string connectionId, string hubId, CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNullOrEmpty(hubId, nameof(hubId));

            using (BeginHubScope(RelayServiceScopes.MethodLeaveHub, hubId, connectionId))
            {
                Logger.LogDebug("Leaving from API");
            }

            var relayHub = GetRelayHub(hubId);
            this.connectionHubs.AddOrUpdate(connectionId, (hubs) => hubs.TryRemove(relayHub));

            await relayHub.LeaveAsync(connectionId, cancellationToken);
            await Task.WhenAll(AllGroups.Select(g => g.RemoveFromGroupAsync(connectionId, hubId, cancellationToken)));
        }

        public Task SendDataHubAsync(
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
                Logger.LogDebug($"sendOption:{sendOption} targetParticipantIds:{targetParticipantIdsStr} type:{type} data-length:{data.Length}");
            }

            return GetRelayHub(hubId).SendDataAsync(connectionId, sendOption, targetParticipantIds, type, data, cancellationToken);
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

                    await hub.LeaveAsync(connectionId, cancellationToken);
                    await Task.WhenAll(AllGroups.Select(g => g.RemoveFromGroupAsync(connectionId, hub.Id, cancellationToken)));
                }));
            }
        }

        private RelayHub GetRelayHub(string hubId, bool createIfNotExists = false)
        {
            if (createIfNotExists)
            {
                return this.relayHubs.GetOrAdd(hubId, (id) => new RelayHub(this, id));
            }
            else
            {
                if (this.relayHubs.TryGetValue(hubId, out var relayHub))
                {
                    return relayHub;
                }

                throw new HubException($"No relay hub found for:{hubId}");
            }
        }

        public IDisposable BeginHubScope(string method, string hubId, string connectionId)
        {
            return Logger.BeginScope(
                    (LoggerScopeHelpers.MethodScope, method),
                    (HubIdScope, hubId),
                    (ConnectionScope, connectionId));
        }

    }
}
