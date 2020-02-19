// <copyright file="IRelayServiceHub.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Options to send data.
    /// </summary>
    public enum SendOption
    {
        None,
        ExcludeSelf,
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

        Task SendDataHubAsync(
            string hubId,
            SendOption sendOption,
            string[] targetParticipantIds,
            string type,
            byte[] data);

        Task UpdateAsync(string hubId, Dictionary<string, object> properties);
    }

    /// <summary>
    /// Join options.
    /// </summary>
    public struct JoinOptions
    {
        /// <summary>
        /// If create the hub when does not yet exists.
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
}
