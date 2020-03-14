// <copyright file="RelayHub.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
    /// Represent a relay hub instance.
    /// </summary>
    internal class RelayHub
    {
        private ConcurrentDictionary<string, Dictionary<string, object>> participants = new ConcurrentDictionary<string, Dictionary<string, object>>();
        private ConcurrentDictionary<string, Dictionary<string, object>> otherParticipants = new ConcurrentDictionary<string, Dictionary<string, object>>();
        private ConcurrentDictionary<string, RelayHubType> typeRelayHub = new ConcurrentDictionary<string, RelayHubType>();

        public RelayHub(RelayService service, string hubId, bool isBackplaneHub)
        {
            Service = Requires.NotNull(service, nameof(service));
            Id = hubId;
            IsBackplaneHub = isBackplaneHub;
        }

        public string Id { get; }

        public Dictionary<string, Dictionary<string, object>> Participants
        {
            get
            {
                return this.participants.Select(kvp => kvp)
                    .Union(this.otherParticipants)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }

        internal bool IsBackplaneHub { get; }

        internal string GroupName => $"{Service.ServiceId}.{Id}";

        private RelayService Service { get; }

        public void SetOtherParticipants(Dictionary<string, Dictionary<string, object>> otherParticipants)
        {
            Requires.NotNull(otherParticipants, nameof(otherParticipants));
            this.otherParticipants = new ConcurrentDictionary<string, Dictionary<string, object>>(otherParticipants);
        }

        public async Task<Dictionary<string, Dictionary<string, object>>> JoinAsync(string participantId, Dictionary<string, object> properties, CancellationToken cancellationToken)
        {
            this.participants.AddOrUpdate(
                participantId,
                properties,
                (k, v) => properties);

            await NotifyParticipantChangedAsync(participantId, properties, ParticipantChangeType.Added, cancellationToken);
            return Participants;
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
            if (!this.participants.ContainsKey(participantId))
            {
                throw new ArgumentException($"participantId:{participantId} not joined");
            }

            this.participants.AddOrUpdate(
                participantId,
                properties,
                (k, v) => properties);
            return NotifyParticipantChangedAsync(participantId, properties, ParticipantChangeType.Updated, cancellationToken);
        }

        public async Task<int> SendDataAsync(
            string fromParticipantId,
            int? messageUniqueId,
            SendOption sendOption,
            string[] targetParticipantIds,
            string type,
            byte[] data,
            Dictionary<string, object> messageProperties,
            CancellationToken cancellationToken)
        {
            var relayHubType = this.typeRelayHub.GetOrAdd(
                type,
                (type) =>
                {
                    return new RelayHubType();
                });

            var uniqueId = messageUniqueId.HasValue ? messageUniqueId.Value : relayHubType.NextUniquId();

            Func<IEnumerable<IClientProxy>, Task> sendTaskCallback = (clients) => Task.WhenAll(clients.Select(proxy => proxy.SendAsync(
                   RelayHubMethods.MethodReceiveData,
                   Id,
                   fromParticipantId,
                   uniqueId,
                   type,
                   data,
                   messageProperties,
                   cancellationToken)));

            try
            {
                if (sendOption.HasFlag(SendOption.Serialize))
                {
                    await relayHubType.WaitAsync(cancellationToken);
                }

                if (targetParticipantIds == null)
                {
                    await sendTaskCallback(sendOption.HasFlag(SendOption.ExcludeSelf) ? AllExcept(fromParticipantId) : All());
                }
                else
                {
                    await sendTaskCallback(All(targetParticipantIds));
                }

                return uniqueId;
            }
            finally
            {
                if (sendOption.HasFlag(SendOption.Serialize))
                {
                    relayHubType.Release();
                }
            }
        }

        public Task NotifyParticipantChangedAsync(
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

        public Task NotifyHubDeletedAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(All().Select(clientProxy => clientProxy.SendAsync(
                RelayHubMethods.MethodHubDeleted,
                Id,
                cancellationToken)));
        }

        public async Task NotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken)
        {
            if (!this.participants.ContainsKey(dataChanged.ParticipantId))
            {
                if (dataChanged.ChangeType == ParticipantChangeType.Removed)
                {
                    this.otherParticipants.TryRemove(dataChanged.ParticipantId, out var properties);
                }
                else
                {
                    this.otherParticipants.AddOrUpdate(
                        dataChanged.ParticipantId,
                        dataChanged.Properties,
                        (k, v) => dataChanged.Properties);
                }
            }

            await NotifyParticipantChangedAsync(
                    dataChanged.ParticipantId,
                    dataChanged.Properties,
                    dataChanged.ChangeType,
                    cancellationToken);
        }

        private IEnumerable<IClientProxy> All()
        {
            return Service.All(GroupName);
        }

        private IEnumerable<IClientProxy> AllExcept(string connectionId)
        {
            return Service.AllExcept(GroupName, new string[] { connectionId });
        }

        private IEnumerable<IClientProxy> All(string[] connectionIds)
        {
            var excludedConnectionIds = this.participants.Keys.Except(connectionIds).ToArray();
            return Service.AllExcept(GroupName, excludedConnectionIds);
        }

        private class RelayHubType
        {
            private int uniqueId;
            private SemaphoreSlim semaphore = new SemaphoreSlim(1);

            public int NextUniquId()
            {
                return Interlocked.Increment(ref this.uniqueId);
            }

            public Task WaitAsync(CancellationToken cancellationToken)
            {
                return this.semaphore.WaitAsync(cancellationToken);
            }

            public void Release()
            {
                this.semaphore.Release();
            }
        }
    }
}
