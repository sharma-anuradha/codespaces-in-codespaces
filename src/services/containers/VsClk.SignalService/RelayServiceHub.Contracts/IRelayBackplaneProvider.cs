// <copyright file="IRelayBackplaneProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RelayHubInfo = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

namespace Microsoft.VsCloudKernel.SignalService
{
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

    /// <summary>
    /// The realy data changed delegate.
    /// </summary>
    /// <typeparam name="T">Type of relay data changed</typeparam>
    /// <param name="relayDataChanged">Instance of the relay data that changed</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public delegate Task OnRelayDataChangedAsync<T>(
            T relayDataChanged,
            CancellationToken cancellationToken)
        where T : RelayDataChanged;

    public enum RelayHubChangeType
    {
        Created,

        Removed,
    }

    /// <summary>
    /// Base relay backplane provider.
    /// </summary>
    public interface IRelayBackplaneProviderBase
    {
        Task<RelayHubInfo> GetRelayInfoAsync(string hubId, CancellationToken cancellationToken);

        Task SendDataHubAsync(SendRelayDataHub dataChanged, CancellationToken cancellationToken);

        Task NotifyParticipantChangedAsync(RelayParticipantChanged dataChanged, CancellationToken cancellationToken);

        Task NotifyRelayHubChangedAsync(RelayHubChanged dataChanged, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Interface to surface a relay backplane provider
    /// </summary>
    public interface IRelayBackplaneProvider : IBackplaneProviderBase<RelayServiceMetrics>, IRelayBackplaneProviderBase
    {
        OnRelayDataChangedAsync<RelayParticipantChanged> ParticipantChangedAsync { get; set; }

        OnRelayDataChangedAsync<SendRelayDataHub> SendDataChangedAsync { get; set; }

        OnRelayDataChangedAsync<RelayHubChanged> RelayHubChanged { get; set; }
    }

    /// <summary>
    /// Provider for a relay backplane manager.
    /// </summary>
    public interface IRelayBackplaneManagerProvider
    {
        Task UpdateRelayHubInfo(string hubId, RelayHubInfo relayHubInfo, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Base class for our data changed structures.
    /// </summary>
    public class RelayDataChanged : DataChanged
    {
        protected RelayDataChanged(string changeId, string serviceId, string hubId)
            : base(changeId)
        {
            Requires.NotNullOrEmpty(serviceId, nameof(serviceId));
            Requires.NotNullOrEmpty(hubId, nameof(hubId));

            ServiceId = serviceId;
            HubId = hubId;
        }

        /// <summary>
        /// The service who originate the change.
        /// </summary>
        public string ServiceId { get; }

        /// <summary>
        /// The referenced hub id.
        /// </summary>
        public string HubId { get; }
    }

    /// <summary>
    /// Class to describe a participant change.
    /// </summary>
    public sealed class RelayParticipantChanged : RelayDataChanged
    {
        public RelayParticipantChanged(
            string changeId,
            string serviceId,
            string hubId,
            string participantId,
            Dictionary<string, object> properties,
            ParticipantChangeType changeType)
            : base(changeId, serviceId, hubId)
        {
            Requires.NotNullOrEmpty(participantId, nameof(participantId));

            ParticipantId = participantId;
            Properties = properties;
            ChangeType = changeType;
        }

        public string ParticipantId { get; }

        public Dictionary<string, object> Properties { get; }

        public ParticipantChangeType ChangeType { get; }
    }

    /// <summary>
    /// The send relay data hub.
    /// </summary>
    public class SendRelayDataHub : RelayDataChanged
    {
        public SendRelayDataHub(
            string changeId,
            string serviceId,
            string hubId,
            SendOption sendOption,
            string fromParticipantId,
            string[] targetParticipantIds,
            string type,
            byte[] data)
            : base(changeId, serviceId, hubId)
        {
            SendOption = sendOption;
            FromParticipantId = fromParticipantId;
            TargetParticipantIds = targetParticipantIds;
            Type = type;
            Data = data;
        }

        /// <summary>
        /// Send option used
        /// </summary>
        public SendOption SendOption { get; }

        /// <summary>
        /// The source participant.
        /// </summary>
        public string FromParticipantId { get; }

        /// <summary>
        /// The target participants ids.
        /// </summary>
        public string[] TargetParticipantIds { get; }

        /// <summary>
        /// Type of the message.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Raw relay data.
        /// </summary>
        public byte[] Data { get; }
    }

    /// <summary>
    /// Class to describe a participant change.
    /// </summary>
    public class RelayHubChanged : RelayDataChanged
    {
        public RelayHubChanged(
            string changeId,
            string serviceId,
            string hubId,
            RelayHubChangeType changeType)
            : base(changeId, serviceId, hubId)
        {
            ChangeType = changeType;
        }

        public RelayHubChangeType ChangeType { get; }
    }

#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
}
