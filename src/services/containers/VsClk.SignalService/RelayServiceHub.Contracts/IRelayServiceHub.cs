// <copyright file="IRelayServiceHub.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Options to send data.
    /// </summary>
    [Flags]
    public enum SendOption
    {
        /// <summary>
        /// None option.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicate to exclude the self participant to notify the data.
        /// </summary>
        ExcludeSelf = 1,

        /// <summary>
        /// Indicate to send serialized data
        /// </summary>
        Serialize = 2,
    }

    /// <summary>
    /// Service exposed by the relay hub.
    /// </summary>
    public interface IRelayServiceHub
    {
        Task<string> CreateHubAsync(string hubId);

        Task DeleteHubAsync(string hubId);

        Task<JoinHubInfo> JoinHubAsync(string hubId, Dictionary<string, object> properties, JoinOptions joinOptions);

        Task LeaveHubAsync(string hubId);

        Task<int> SendDataHubAsync(
            string hubId,
            SendOption sendOption,
            string[] targetParticipantIds,
            string type,
            byte[] data,
            Dictionary<string, object> messageProperties);

        Task UpdateAsync(string hubId, Dictionary<string, object> properties);
    }

    /// <summary>
    /// Join options.
    /// </summary>
    public struct JoinOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether create the hub when does not yet exists.
        /// </summary>
        public bool CreateIfNotExists { get; set; }
    }

    /// <summary>
    /// A hub participant.
    /// </summary>
    public struct HubParticipant
    {
        public string Id { get; set; }

        public Dictionary<string, object> Properties { get; set; }
    }

    /// <summary>
    /// The joined hub info.
    /// </summary>
    public struct JoinHubInfo
    {
        public string ServiceId { get; set; }

        public string Stamp { get; set; }

        public string ParticipantId { get; set; }

        public HubParticipant[] Participants { get; set; }
    }

    /// <summary>
    /// Data to pass to the send hub data.
    /// </summary>
    public struct SendHubData
    {
        public string HubId { get; set; }

        public int SendOption { get; set; }

        public string[] TargetParticipantIds { get; set; }

        public string Type { get; set; }

        public Dictionary<string, object> MessageProperties { get; set; }
    }
}
