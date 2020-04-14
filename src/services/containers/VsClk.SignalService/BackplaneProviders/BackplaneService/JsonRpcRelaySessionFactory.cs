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
            BackplaneService.SendDataHubAsync(dataChanged, cancellationToken);

        public Task NotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken) =>
            BackplaneService.NotifyParticipantChangedAsync(dataChanged, cancellationToken);

        public Task NotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken) =>
            BackplaneService.NotifyRelayHubChangedAsync(dataChanged, cancellationToken);

        public Task<Dictionary<string, Dictionary<string, object>>> GetRelayInfoAsync(string hubId, CancellationToken cancellationToken) =>
            BackplaneService.GetRelayInfoAsync(hubId, cancellationToken);
    }
}
