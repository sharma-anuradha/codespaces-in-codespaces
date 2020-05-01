// <copyright file="JsonRpcRelaySessionFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// The json rpc relay session factory.
    /// </summary>
    public class JsonRpcRelaySessionFactory : JsonRpcSessionFactory<RelayBackplaneService, IRelayBackplaneManager, IRelayBackplaneServiceNotification>, IRelayBackplaneServiceNotification
    {
        public JsonRpcRelaySessionFactory(RelayBackplaneService backplaneService, ILogger<JsonRpcRelaySessionFactory> logger)
            : base(backplaneService, logger)
        {
        }

        /// <inheritdoc/>
        public override string ServiceType => "relay";

        /// <inheritdoc/>
        Task IRelayBackplaneServiceNotification.FireSendDataHubAsync(SendRelayDataHub dataChanged, CancellationToken cancellationToken)
        {
            return InvokeAllAsync(nameof(IRelayBackplaneServiceNotification.FireSendDataHubAsync), dataChanged);
        }

        /// <inheritdoc/>
        Task IRelayBackplaneServiceNotification.FireNotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken)
        {
            return InvokeAllAsync(nameof(IRelayBackplaneServiceNotification.FireNotifyParticipantChangedAsync), dataChanged);
        }

        /// <inheritdoc/>
        Task IRelayBackplaneServiceNotification.FireNotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken)
        {
            return InvokeAllAsync(nameof(IRelayBackplaneServiceNotification.FireNotifyRelayHubChangedAsync), dataChanged);
        }

        public Task SendDataHubAsync(SendRelayDataHub dataChanged, CancellationToken cancellationToken) =>
            InvokeBackplaneService(b => b.SendDataHubAsync(dataChanged, cancellationToken), nameof(SendDataHubAsync));

        public Task NotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken) =>
            InvokeBackplaneService(b => b.NotifyParticipantChangedAsync(dataChanged, cancellationToken), nameof(NotifyParticipantChangedAsync));

        public Task NotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken) =>
            InvokeBackplaneService(b => b.NotifyRelayHubChangedAsync(dataChanged, cancellationToken), nameof(NotifyRelayHubChangedAsync));

        public Task<Dictionary<string, Dictionary<string, object>>> GetRelayInfoAsync(string hubId, CancellationToken cancellationToken) =>
            InvokeBackplaneService(b => b.GetRelayInfoAsync(hubId, cancellationToken), nameof(GetRelayInfoAsync));
    }
}
