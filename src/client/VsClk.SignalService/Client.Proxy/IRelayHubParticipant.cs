// <copyright file="IRelayHubParticipant.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// A relay hub participant.
    /// </summary>
    public interface IRelayHubParticipant
    {
        /// <summary>
        /// Gets id of the participant.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets properties publsihed by this participant.
        /// </summary>
        Dictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets a value indicating whether if participant is self.
        /// </summary>
        bool IsSelf { get; }
    }
}
