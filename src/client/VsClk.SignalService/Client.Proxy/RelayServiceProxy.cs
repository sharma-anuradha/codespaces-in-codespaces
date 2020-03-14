// <copyright file="RelayServiceProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
            : base(hubProxy, trace, formatProvider)
        {
            hubProxy.ConnectionStateChanged += (s, e) =>
            {
                if (!hubProxy.IsConnected)
                {
                    foreach (var relayHubProxy in this.relayHubs.Values)
                    {
                        trace.Verbose($"OnHubDisconnected-> hubId:{relayHubProxy.Id}");
                        relayHubProxy.OnHubDisconnected();
                    }
                }

                this.relayHubs.Clear();

                return Task.CompletedTask;
            };

            AddHubHandler(hubProxy.On(
                RelayHubMethods.MethodReceiveData,
                new Type[] { typeof(string), typeof(string), typeof(int), typeof(string), typeof(byte[]), typeof(Dictionary<string, object>) },
                (args) =>
            {
                var hubId = (string)args[0];
                var fromParticipantId = (string)args[1];
                var uniqueId = ToProxyType<int>(args[2]);
                var type = (string)args[3];
                var data = (byte[])args[4];
                var messageProperties = ToProxyDictionary(args[5]);

                if (TraceHubData)
                {
                    trace.Verbose($"ReceiveData-> hubId:{hubId} from:{fromParticipantId:T} uniqueId:{uniqueId} type:{type} data-length:{data?.Length} properties:{messageProperties.ConvertToString(FormatProvider)}");
                }

                if (this.relayHubs.TryGetValue(hubId, out var relayHubProxy))
                {
                    relayHubProxy.OnReceiveData(fromParticipantId, uniqueId, type, data, messageProperties);
                }
                else
                {
                    trace.Warning($"No relay hub attached for hubId:{hubId}");
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
                var properties = ToProxyDictionary(args[2]);
                var changeType = ToProxyEnum<ParticipantChangeType>(args[3]);

                trace.Verbose($"ParticipantChanged-> hubId:{hubId} participantId:{participantId:T} properties:{properties.ConvertToString(FormatProvider)} changeType:{changeType}");
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
        public bool TraceHubData { get; set; }

        /// <inheritdoc/>
        public Task<string> CreateHubAsync(string hubId, CancellationToken cancellationToken)
        {
            return HubProxy.InvokeAsync<string>(nameof(IRelayServiceHub.CreateHubAsync), new object[] { hubId }, cancellationToken);
        }

        /// <inheritdoc/>
        public Task DeleteHubAsync(string hubId, CancellationToken cancellationToken)
        {
            return HubProxy.InvokeAsync<object>(nameof(IRelayServiceHub.DeleteHubAsync), new object[] { hubId }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IRelayHubProxy> JoinHubAsync(string hubId, Dictionary<string, object> properties, JoinOptions joinOptions, CancellationToken cancellationToken)
        {
            RelayHubProxy relayHubProxy;
            if (this.relayHubs.TryGetValue(hubId, out relayHubProxy))
            {
                if (relayHubProxy.IsDisconnected)
                {
                    await relayHubProxy.ReJoinInternalAsync(properties, joinOptions, cancellationToken);
                }
                else if (properties != null)
                {
                    await relayHubProxy.UpdateAsync(properties, cancellationToken);
                }
            }
            else
            {
                var joinHubInfo = await JoinHubProxyAsync(hubId, properties, joinOptions, cancellationToken);
                relayHubProxy = new RelayHubProxy(this, hubId, joinHubInfo);
                this.relayHubs[hubId] = relayHubProxy;
            }

            return relayHubProxy;
        }

        private async Task<JoinHubInfo> JoinHubProxyAsync(string hubId, Dictionary<string, object> properties, JoinOptions joinOptions, CancellationToken cancellationToken)
        {
            var joinHubInfo = await HubProxy.InvokeAsync<JoinHubInfo>(nameof(IRelayServiceHub.JoinHubAsync), new object[] { hubId, properties, joinOptions }, cancellationToken);
            Trace.Verbose($"JoinHubProxyAsync hubId:{hubId} -> stamp:{joinHubInfo.Stamp} service Id:{joinHubInfo.ServiceId} self id:{joinHubInfo.ParticipantId} participants:[{string.Join(",", joinHubInfo.Participants.Select(p => p.Id))}]");
            return joinHubInfo;
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
                SetJoinInfo(joinHubInfo);
            }

            public event EventHandler<ReceiveDataEventArgs> ReceiveData;

            public event EventHandler<ParticipantChangedEventArgs> ParticipantChanged;

            public event EventHandler Disconnected;

            public event EventHandler Deleted;

            public IRelayHubParticipant SelfParticipant { get; private set; }

            public IRelayServiceProxy RelayServiceProxy => this.relayServiceProxy;

            public string ServiceId { get; }

            public string Stamp { get; }

            public string Id { get; }

            public IEnumerable<IRelayHubParticipant> Participants => this.hubParticipants.Values;

            public bool IsDisconnected { get; private set; }

            private TraceSource Trace => this.relayServiceProxy.Trace;

            private IHubProxy HubProxy => this.relayServiceProxy.HubProxy;

            public async Task DisposeAsync()
            {
                if (!this.isDisposed)
                {
                    this.isDisposed = true;
                    this.relayServiceProxy.relayHubs.TryRemove(Id, out var _);
                    await HubProxy.InvokeAsync<object>(nameof(IRelayServiceHub.LeaveHubAsync), new object[] { Id }, default(CancellationToken));
                }
            }

            public async Task<int> SendDataAsync(
                SendOption sendOption,
                string[] targetParticipantIds,
                string type,
                byte[] data,
                Dictionary<string, object> messageProperties,
                HubMethodOption methodOption,
                CancellationToken cancellationToken)
            {
                CheckState();
                if (this.relayServiceProxy.TraceHubData)
                {
                    Trace.Verbose($"SendData-> hubId:{Id} type:{type} data-length:{data?.Length} properties:{messageProperties.ConvertToString(this.relayServiceProxy.FormatProvider)}");
                }

                if (methodOption == HubMethodOption.Send)
                {
                    await HubProxy.SendAsync(nameof(IRelayServiceHub.SendDataHubAsync), new object[] { Id, sendOption, targetParticipantIds, type, data, messageProperties }, cancellationToken).ConfigureAwait(false);
                    return 0;
                }
                else
                {
                    var uniqueId = await HubProxy.InvokeAsync<int>(nameof(IRelayServiceHub.SendDataHubAsync), new object[] { Id, sendOption, targetParticipantIds, type, data, messageProperties }, cancellationToken);
                    if (this.relayServiceProxy.TraceHubData)
                    {
                        Trace.Verbose($"SendData <- result:{uniqueId}");
                    }

                    return uniqueId;
                }
            }

            public Task UpdateAsync(
                Dictionary<string, object> properties,
                CancellationToken cancellationToken)
            {
                CheckState();

                return HubProxy.InvokeAsync<object>(nameof(IRelayServiceHub.UpdateAsync), new object[] { Id, properties }, cancellationToken);
            }

            public async Task ReJoinAsync(JoinOptions joinOptions, CancellationToken cancellationToken)
            {
                if (!(IsDisconnected || this.isDisposed))
                {
                    throw new InvalidOperationException($"Relay hub:{Id} is connected");
                }

                await ReJoinInternalAsync(SelfParticipant.Properties, joinOptions, cancellationToken);
                this.relayServiceProxy.relayHubs[Id] = this;
            }

            public async Task ReJoinInternalAsync(Dictionary<string, object> properties, JoinOptions joinOptions, CancellationToken cancellationToken)
            {
                var joinHubInfo = await this.relayServiceProxy.JoinHubProxyAsync(Id, properties, joinOptions, cancellationToken);
                SetJoinInfo(joinHubInfo);
                IsDisconnected = false;
                this.isDisposed = false;
            }

            internal void OnReceiveData(string fromParticipantId, int uniqueId, string type, byte[] data, Dictionary<string, object> messageProperties)
            {
                if (this.hubParticipants.TryGetValue(fromParticipantId, out var relayHubParticipant))
                {
                    ReceiveData?.Invoke(this, new ReceiveDataEventArgs(relayHubParticipant, uniqueId, type, data, messageProperties));
                }
            }

            internal void OnParticipantChanged(string participantId, Dictionary<string, object> properties, ParticipantChangeType changeType)
            {
                RelayHubParticipant relayHubParticipant = null;
                if (changeType == ParticipantChangeType.Added)
                {
                    relayHubParticipant = new RelayHubParticipant(participantId, properties);
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

            internal void OnHubDisconnected()
            {
                IsDisconnected = true;
                Disconnected?.Invoke(this, EventArgs.Empty);
            }

            private void SetJoinInfo(JoinHubInfo joinHubInfo)
            {
                this.hubParticipants.Clear();
                foreach (var hubParticipant in joinHubInfo.Participants)
                {
                    var relayHubParticipant = new RelayHubParticipant(hubParticipant.Id, hubParticipant.Properties);
                    if (joinHubInfo.ParticipantId == hubParticipant.Id)
                    {
                        SelfParticipant = relayHubParticipant;
                    }

                    this.hubParticipants[hubParticipant.Id] = relayHubParticipant;
                }
            }

            private void CheckState()
            {
                if (IsDisconnected)
                {
                    throw new InvalidOperationException($"Relay hub:{Id} disconnected");
                }
                else if (this.isDisposed)
                {
                    throw new ObjectDisposedException($"Relay hub:{Id}");
                }
            }

            private class RelayHubParticipant : IRelayHubParticipant
            {
                internal RelayHubParticipant(string id, Dictionary<string, object> properties)
                {
                    Id = id;
                    Properties = properties;
                }

                public string Id { get; }

                public Dictionary<string, object> Properties { get; set; }
            }
        }
    }
}
