// <copyright file="ParticipantChangedEventArgs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Event to report a participant changed event.
    /// </summary>
    public class ParticipantChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParticipantChangedEventArgs"/> class.
        /// </summary>
        /// <param name="participant">The hub participant.</param>
        /// <param name="changeType">Type of change.</param>
        internal ParticipantChangedEventArgs(
            IRelayHubParticipant participant,
            ParticipantChangeType changeType)
        {
            Participant = participant;
            ChangeType = changeType;
        }

        /// <summary>
        /// Gets participant that changed.
        /// </summary>
        public IRelayHubParticipant Participant { get; }

        /// <summary>
        /// Gets the change type.
        /// </summary>
        public ParticipantChangeType ChangeType { get; }
    }
}
