// <copyright file="ReceiveDataEventArgs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Event to report a data being received from a hub.
    /// </summary>
    public class ReceiveDataEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveDataEventArgs"/> class.
        /// </summary>
        /// <param name="fromParticipant">Which participant send the data.</param>
        /// <param name="uniqueId">Unique id of this data.</param>
        /// <param name="type">Type of data.</param>
        /// <param name="data">Raw data being sent.</param>
        /// <param name="messageProperties">Message properties.</param>
        internal ReceiveDataEventArgs(
            IRelayHubParticipant fromParticipant,
            int uniqueId,
            string type,
            byte[] data,
            Dictionary<string, object> messageProperties)
        {
            FromParticipant = fromParticipant;
            UniqueId = uniqueId;
            Type = type;
            Data = data;
            MessageProperties = messageProperties;
        }

        /// <summary>
        /// Gets which participant send the data.
        /// </summary>
        public IRelayHubParticipant FromParticipant { get; }

        /// <summary>
        /// Gets unique id of this data.
        /// </summary>
        public int UniqueId { get; }

        /// <summary>
        /// Gets type of data.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets raw data being received.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// Gets the message properties.
        /// </summary>
        public Dictionary<string, object> MessageProperties { get; }
    }
}
