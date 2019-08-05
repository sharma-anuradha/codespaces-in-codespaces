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
        private ConcurrentDictionary<string, RelayHub> relayHubs = new ConcurrentDictionary<string, RelayHub>();
        private ConcurrentDictionary<string, ConcurrentHashSet<RelayHub>> connectionHubs = new ConcurrentDictionary<string, ConcurrentHashSet<RelayHub>>();

        public RelayService(
            RelayServiceOptions options,
            IEnumerable<IHubContextHost> hubContextHosts,
            ILogger<RelayService> logger)
            : base(options.Id, hubContextHosts, logger)
        {
            if (HubContextHosts.Length != 1)
            {
                throw new ArgumentException("Expected one realy hub context");
            }
        }

        internal IHubContextHost Hub => HubContextHosts[0];

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

            using (Logger.BeginMethodScope(RelayServiceScopes.MethodJoinHub))
            {
                Logger.LogDebug($"connectionId:{connectionId} hubId:{hubId} properties:{properties.ConvertToString()}");
            }

            var relayHub = GetRelayHub(hubId, createIfNotExists);
            this.connectionHubs.AddOrUpdate(connectionId, (hubs) => hubs.Add(relayHub));
            await Hub.Groups.AddToGroupAsync(connectionId, hubId, cancellationToken);
            return await relayHub.JoinAsync(connectionId, properties, cancellationToken);
        }

        public async Task LeaveHubAsync(string connectionId, string hubId, CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNullOrEmpty(hubId, nameof(hubId));

            using (Logger.BeginMethodScope(RelayServiceScopes.MethodLeaveHub))
            {
                Logger.LogDebug($"connectionId:{connectionId} hubId:{hubId}");
            }

            var relayHub = GetRelayHub(hubId);
            this.connectionHubs.AddOrUpdate(connectionId, (hubs) => hubs.TryRemove(relayHub));

            await relayHub.LeaveAsync(connectionId, cancellationToken);
            await Hub.Groups.RemoveFromGroupAsync(connectionId, hubId, cancellationToken);
        }

        public Task SendDataHubAsync(
            string connectionId,
            string hubId,
            string[] targetParticipantIds,
            string type,
            byte[] data,
            CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNullOrEmpty(hubId, nameof(hubId));
            Requires.NotNullOrEmpty(type, nameof(type));
            Requires.NotNull(data, nameof(data));

            using (Logger.BeginMethodScope(RelayServiceScopes.MethodSendDataHub))
            {
                var targetParticipantIdsStr = targetParticipantIds != null ? string.Join(",", targetParticipantIds) : "*";
                Logger.LogDebug($"connectionId:{connectionId} hubId:{hubId} targetParticipantIds:{targetParticipantIdsStr} type:{type} data-length:{data.Length}");
            }

            return GetRelayHub(hubId).SendDataAsync(connectionId, targetParticipantIds, type, data, cancellationToken);
        }

        public async Task DisconnectAsync(string connectionId, CancellationToken cancellationToken)
        {
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));

            if (this.connectionHubs.TryRemove(connectionId, out var relayHubs))
            {
                await Task.WhenAll(relayHubs.Values.Select(async hub =>
                {
                    await hub.LeaveAsync(connectionId, cancellationToken);
                    await Hub.Groups.RemoveFromGroupAsync(connectionId, hub.Id, cancellationToken);
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
    }
}
