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
using Microsoft.VsCloudKernel.SignalService.Common;

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
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value != null ? kvp.Value.Clone() : null);
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

        public async Task<(Dictionary<string, Dictionary<string, object>> participants, bool isNewParticipant)> JoinAsync(string participantId, Dictionary<string, object> properties, CancellationToken cancellationToken)
        {
            bool isNewParticipant;
            properties = AddOrUpdateParticipant(this.participants, participantId, properties, out isNewParticipant);

            await NotifyParticipantChangedAsync(participantId, properties, isNewParticipant ? ParticipantChangeType.Added : ParticipantChangeType.Updated, cancellationToken);
            return (Participants, isNewParticipant);
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

            bool isNewParticipant;
            properties = AddOrUpdateParticipant(this.participants, participantId, properties, out isNewParticipant);
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

            // callback to be invoked on each target client
            Func<IEnumerable<IClientProxy>, byte[], Dictionary<string, object>, Task> sendTaskCallback = (clients, clientData, clientMessageProperties) => Task.WhenAll(clients.Select(proxy => proxy.SendAsync(
                   RelayHubMethods.MethodReceiveData,
                   Id,
                   fromParticipantId,
                   uniqueId,
                   type,
                   clientData,
                   clientMessageProperties,
                   cancellationToken)));

            try
            {
                if (sendOption.HasFlag(SendOption.Serialize))
                {
                    await relayHubType.WaitAsync(cancellationToken);
                }

                if (targetParticipantIds == null || targetParticipantIds.Length == 0)
                {
                    await sendTaskCallback(sendOption.HasFlag(SendOption.ExcludeSelf) ? AllExcept(fromParticipantId) : All(), data, messageProperties);
                }
                else
                {
                    if (sendOption.HasFlag(SendOption.Batch))
                    {
                        // Note: the payload will contain data for each target on the message properties.
                        await Task.WhenAll(targetParticipantIds.Select(participantId =>
                        {
                            var targetPrefixProperty = $"{RelayHubMessageProperties.PropertyTargetPrefixId}{participantId}-";
                            byte[] clientData = null;
                            var clientMessageProperties = messageProperties.Where(kvp =>
                            {
                                if (kvp.Key.StartsWith(targetPrefixProperty))
                                {
                                    var property = kvp.Key.Substring(targetPrefixProperty.Length);
                                    if (property == RelayHubMessageProperties.PropertyDataId)
                                    {
                                        clientData = (byte[])kvp.Value;
                                        return false;
                                    }

                                    return true;
                                }
                                else
                                {
                                    return !kvp.Key.StartsWith(RelayHubMessageProperties.PropertyTargetPrefixId);
                                }
                            }).ToDictionary(kvp => kvp.Key.StartsWith(targetPrefixProperty) ? kvp.Key.Substring(targetPrefixProperty.Length) : kvp.Key, kvp => kvp.Value);

                            return sendTaskCallback(All(new string[] { participantId }), clientData, clientMessageProperties);
                        }));
                    }
                    else
                    {
                        await sendTaskCallback(All(targetParticipantIds), data, messageProperties);
                    }
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
                Dictionary<string, object> properties;
                if (dataChanged.ChangeType == ParticipantChangeType.Removed)
                {
                    this.otherParticipants.TryRemove(dataChanged.ParticipantId, out properties);
                }
                else
                {
                    properties = AddOrUpdateParticipant(
                        this.otherParticipants,
                        dataChanged.ParticipantId,
                        dataChanged.Properties,
                        out var isNewParticipant);
                }

                await NotifyParticipantChangedAsync(
                        dataChanged.ParticipantId,
                        properties,
                        dataChanged.ChangeType,
                        cancellationToken);
            }
        }

        private static Dictionary<string, object> AddOrUpdateParticipant(
            ConcurrentDictionary<string, Dictionary<string, object>> participants,
            string participantId,
            Dictionary<string, object> properties,
            out bool isNewParticipant)
        {
            Assumes.NotNull(participants);

            bool added = true;
            properties = participants.AddOrUpdate(
                participantId,
                properties,
                (k, participantProperties) =>
                {
                    added = false;

                    if (participantProperties != null)
                    {
                        // merge the properties
                        if (properties != null)
                        {
                            foreach (var kvp in properties)
                            {
                                participantProperties[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    else
                    {
                        participantProperties = properties;
                    }

                    return participantProperties;
                })?.Clone();
            isNewParticipant = added;
            return properties;
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
