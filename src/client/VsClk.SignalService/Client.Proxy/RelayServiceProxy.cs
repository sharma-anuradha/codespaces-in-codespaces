// <copyright file="RelayServiceProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// The relay service proxy client that connects to the remote Hub.
    /// </summary>
    public class RelayServiceProxy : ProxyBase, IRelayServiceProxy
    {
        /// <summary>
        /// Name of the remote hub.
        /// </summary>
        public const string HubName = "relayServiceHub";

        private readonly ConcurrentDictionary<string, RelayHubProxy> relayHubs = new ConcurrentDictionary<string, RelayHubProxy>();
        private readonly IDataFormatProvider formatProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayServiceProxy"/> class.
        /// </summary>
        /// <param name="hubProxy">The hub proxy instance.</param>
        /// <param name="trace">Trace instance.</param>
        public RelayServiceProxy(IHubProxy hubProxy, TraceSource trace)
            : this(hubProxy, trace, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayServiceProxy"/> class.
        /// </summary>
        /// <param name="hubProxy">The hub proxy instance.</param>
        /// <param name="trace">Trace instance.</param>
        /// <param name="formatProvider">Optional format provider.</param>
        public RelayServiceProxy(IHubProxy hubProxy, TraceSource trace, IFormatProvider formatProvider)
            : base(hubProxy)
        {
            Requires.NotNull(trace, nameof(trace));
            this.formatProvider = formatProvider != null ? DataFormatProvider.Create(formatProvider) : null;

            AddHubHandler(hubProxy.On(
                RelayHubMethods.MethodReceiveData,
                new Type[] { typeof(string), typeof(string), typeof(int), typeof(string), typeof(byte[]) },
                (args) =>
            {
                var hubId = (string)args[0];
                var fromParticipantId = (string)args[1];
                var uniqueId = (int)args[2];
                var type = (string)args[3];
                var data = (byte[])args[4];

                trace.Verbose($"ReceiveData-> hubId:{hubId} from:{fromParticipantId:T} uniqueId:{uniqueId} type:{type} data-length:{data.Length}");
                if (this.relayHubs.TryGetValue(hubId, out var relayHubProxy))
                {
                    relayHubProxy.OnReceiveData(fromParticipantId, uniqueId, type, data);
                }

                return Task.CompletedTask;
            }));

            AddHubHandler(hubProxy.On(
                RelayHubMethods.MethodParticipantChanged,
                new Type[] { typeof(string), typeof(string), typeof(Dictionary<string, object>), typeof(ParticipantChangeType) },
                (args) =>
            {
                var hubId = (string)args[0];
                var participantId = (string)args[1];
                var properties = (Dictionary<string, object>)args[2];
                var changeType = (ParticipantChangeType)args[3];

                trace.Verbose($"ParticipantChanged-> hubId:{hubId} participantId:{participantId:T} properties:{properties.ConvertToString(this.formatProvider)} changeType:{changeType}");
                if (this.relayHubs.TryGetValue(hubId, out var relayHubProxy))
                {
                    relayHubProxy.OnParticipantChanged(participantId, properties, changeType);
                }

                return Task.CompletedTask;
            }));

            AddHubHandler(hubProxy.On(
                RelayHubMethods.MethodHubDeleted,
                new Type[] { typeof(string) },
                (args) =>
                {
                    var hubId = (string)args[0];

                    trace.Verbose($"Hub deleted-> hubId:{hubId}");
                    if (this.relayHubs.TryRemove(hubId, out var relayHubProxy))
                    {
                        relayHubProxy.OnHubDeleted();
                    }

                    return Task.CompletedTask;
            }));
        }

        /// <inheritdoc/>
        public Task<string> CreateHubAsync(string hubId, CancellationToken cancellationToken)
        {
            return HubProxy.InvokeAsync<string>(nameof(IRelayServiceHub.CreateHubAsync), new object[] { hubId }, cancellationToken);
        }

        /// <inheritdoc/>
        public Task DeleteHubAsync(string hubId, CancellationToken cancellationToken)
        {
            return HubProxy.InvokeAsync(nameof(IRelayServiceHub.DeleteHubAsync), new object[] { hubId }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IRelayHubProxy> JoinHubAsync(string hubId, Dictionary<string, object> properties, bool createIfNotExists, CancellationToken cancellationToken)
        {
            var joinHub = await HubProxy.InvokeAsync<JoinHubInfo>(nameof(IRelayServiceHub.JoinHubAsync), new object[] { hubId, properties, createIfNotExists }, cancellationToken);
            var relayHubProxy = new RelayHubProxy(this, hubId, joinHub);
            this.relayHubs[hubId] = relayHubProxy;
            return relayHubProxy;
        }

        private class RelayHubProxy : IRelayHubProxy
        {
            private readonly ConcurrentDictionary<string, RelayHubParticipant> hubParticipants = new ConcurrentDictionary<string, RelayHubParticipant>();
            private readonly RelayServiceProxy relayServiceProxy;
            private bool isDisposed;

            internal RelayHubProxy(RelayServiceProxy relayServiceProxy, string hubId, JoinHubInfo joinHubInfo)
            {
                this.relayServiceProxy = relayServiceProxy;
                ServiceId = joinHubInfo.ServiceId;
                Stamp = joinHubInfo.Stamp;
                Id = hubId;
                foreach (var hubParticipant in joinHubInfo.Participants)
                {
                    this.hubParticipants[hubParticipant.Id] = new RelayHubParticipant(hubParticipant.Id, hubParticipant.Properties, joinHubInfo.ParticipantId == hubParticipant.Id);
                }
            }

            public event EventHandler<ReceiveDataEventArgs> ReceiveData;

            public event EventHandler<ParticipantChangedEventArgs> ParticipantChanged;

            public event EventHandler Deleted;

            public IRelayServiceProxy RelayServiceProxy => this.relayServiceProxy;

            public string ServiceId { get; }

            public string Stamp { get; }

            public string Id { get; }

            public IEnumerable<IRelayHubParticipant> Participants => this.hubParticipants.Values;

            public async Task DisposeAsync()
            {
                if (!this.isDisposed)
                {
                    this.isDisposed = true;
                    this.relayServiceProxy.relayHubs.TryRemove(Id, out var _);
                    await this.relayServiceProxy.HubProxy.InvokeAsync(nameof(IRelayServiceHub.LeaveHubAsync), new object[] { Id }, default(CancellationToken));
                }
            }

            public Task SendDataAsync(
                SendOption sendOption,
                string[] targetParticipantIds,
                string type,
                byte[] data,
                CancellationToken cancellationToken)
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException($"Relay hub:{Id}");
                }

                return this.relayServiceProxy.HubProxy.InvokeAsync(nameof(IRelayServiceHub.SendDataHubAsync), new object[] { Id, sendOption, targetParticipantIds, type, data }, cancellationToken);
            }

            public Task UpdateAsync(
                Dictionary<string, object> properties,
                CancellationToken cancellationToken)
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException($"Relay hub:{Id}");
                }

                return this.relayServiceProxy.HubProxy.InvokeAsync(nameof(IRelayServiceHub.UpdateAsync), new object[] { Id, properties }, cancellationToken);
            }

            internal void OnReceiveData(string fromParticipantId, int uniqueId, string type, byte[] data)
            {
                if (this.hubParticipants.TryGetValue(fromParticipantId, out var relayHubParticipant))
                {
                    ReceiveData?.Invoke(this, new ReceiveDataEventArgs(relayHubParticipant, uniqueId, type, data));
                }
            }

            internal void OnParticipantChanged(string participantId, Dictionary<string, object> properties, ParticipantChangeType changeType)
            {
                RelayHubParticipant relayHubParticipant = null;
                if (changeType == ParticipantChangeType.Added)
                {
                    relayHubParticipant = new RelayHubParticipant(participantId, properties, false);
                    this.hubParticipants[participantId] = relayHubParticipant;
                }
                else if (changeType == ParticipantChangeType.Removed)
                {
                    this.hubParticipants.TryRemove(participantId, out relayHubParticipant);
                }
                else if (changeType == ParticipantChangeType.Updated)
                {
                    if (this.hubParticipants.TryGetValue(participantId, out relayHubParticipant))
                    {
                        relayHubParticipant.Properties = properties;
                    }
                }

                ParticipantChanged?.Invoke(this, new ParticipantChangedEventArgs(relayHubParticipant, changeType));
            }

            internal void OnHubDeleted()
            {
                Deleted?.Invoke(this, EventArgs.Empty);
            }

            private class RelayHubParticipant : IRelayHubParticipant
            {
                internal RelayHubParticipant(string id, Dictionary<string, object> properties, bool isSelf)
                {
                    Id = id;
                    Properties = properties;
                    IsSelf = isSelf;
                }

                public string Id { get; }

                public Dictionary<string, object> Properties { get; set; }

                public bool IsSelf { get; }
            }
        }
    }
}
