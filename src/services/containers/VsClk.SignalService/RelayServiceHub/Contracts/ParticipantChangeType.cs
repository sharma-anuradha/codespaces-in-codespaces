// <copyright file="ParticipantChangeType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Participant change types.
    /// </summary>
    public enum ParticipantChangeType
    {
        /// <summary>
        /// No action.
        /// </summary>
        None,

        /// <summary>
        /// When a participant is added.
        /// </summary>
        Added,

        /// <summary>
        /// When a participant is removed.
        /// </summary>
        Removed,

        /// <summary>
        /// When a participant is updated.
        /// </summary>
        Updated,
    }
}
