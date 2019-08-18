using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Represent a relay hub instance
    /// </summary>
    internal class RelayHub
    {
        private readonly RelayService service;
        private ConcurrentDictionary<string, Dictionary<string, object>> participants = new ConcurrentDictionary<string, Dictionary<string, object>>();
        private ConcurrentDictionary<string, int> typeUniqueId = new ConcurrentDictionary<string, int>();

        public RelayHub(RelayService service, string hubId)
        {
            this.service = Requires.NotNull(service, nameof(service));
            Id = hubId;
        }

        public string Id { get; }

        public async Task<Dictionary<string, Dictionary<string, object>>> JoinAsync(string participantId, Dictionary<string, object> properties, CancellationToken cancellationToken)
        {
            this.participants.AddOrUpdate(
                participantId,
                properties,
                (k, v) => properties);

            await NotifyParticipantChangedAsync(participantId, properties, ParticipantChangeType.Added, cancellationToken);
            return this.participants.Select(kvp => kvp).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public Task LeaveAsync(string participantId, CancellationToken cancellationToken)
        {
            if (this.participants.TryRemove(participantId, out var properties))
            {
                return NotifyParticipantChangedAsync(participantId, properties, ParticipantChangeType.Removed, cancellationToken);
            }

            return Task.CompletedTask;
        }

        public Task UpdateAsync(string participantId, Dictionary<string, object> properties, CancellationToken cancellationToken)
        {
            this.participants.AddOrUpdate(
                participantId,
                properties,
                (k, v) => properties);
            return NotifyParticipantChangedAsync(participantId, properties, ParticipantChangeType.Updated, cancellationToken);
        }

        public Task SendDataAsync(
            string fromParticipantId,
            SendOption sendOption,
            string[] targetParticipantIds,
            string type,
            byte[] data, 
            CancellationToken cancellationToken)
        {
            var uniqueId = this.typeUniqueId.AddOrUpdate(
                type,
                0,
                (k, v) =>
                {
                    ++v;
                    return v;
                });

            Func<IEnumerable<IClientProxy>, Task> sendTaskCallback = (clients) => Task.WhenAll(clients.Select(proxy => proxy.SendAsync(
                   RelayHubMethods.MethodReceiveData,
                   Id,
                   fromParticipantId,
                   uniqueId,
                   type,
                   data)));

            if (targetParticipantIds == null)
            {
                return sendTaskCallback(sendOption == SendOption.ExcludeSelf ? AllExcept(fromParticipantId) : All());
            }
            else
            {
                return Task.WhenAll(targetParticipantIds.Select(id => sendTaskCallback(Clients(id))));
            }
        }

        private Task NotifyParticipantChangedAsync(
            string participantId,
            Dictionary<string, object> properties,
            ParticipantChangeType changeType,
            CancellationToken cancellationToken)
        {
            return Task.WhenAll(All().Select(clientProxy => clientProxy.SendAsync(
                RelayHubMethods.MethodParticipantChanged,
                Id,
                participantId,
                properties,
                changeType,
                cancellationToken)));
        }

        private IEnumerable<IClientProxy> All()
        {
            return this.service.All(Id);
        }

        private IEnumerable<IClientProxy> AllExcept(string connectionId)
        {
            return this.service.AllExcept(Id, new string[] { connectionId });
        }

        private IEnumerable<IClientProxy> Clients(string connectionId)
        {
            return this.service.Clients(connectionId);
        }
    }
}
