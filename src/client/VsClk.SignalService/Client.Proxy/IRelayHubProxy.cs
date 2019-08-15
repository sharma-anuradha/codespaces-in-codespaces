// <copyright file="IRelayHubProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// A relay hub proxy.
    /// </summary>
    public interface IRelayHubProxy : IAsyncDisposable
    {
        /// <summary>
        /// When data is recieved.
        /// </summary>
        event EventHandler<ReceiveDataEventArgs> ReceiveData;

        /// <summary>
        /// When participants changed.
        /// </summary>
        event EventHandler<ParticipantChangedEventArgs> ParticipantChanged;

        /// <summary>
        /// Gets unique id of the hub.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets list of active participants.
        /// </summary>
        IEnumerable<IRelayHubParticipant> Participants { get; }

        /// <summary>
        /// Send raw data to all or some participants.
        /// </summary>
        /// <param name="sendOption">Options to send the data.</param>
        /// <param name="targetParticipantIds">Which target participants to send.</param>
        /// <param name="type">Type of data.</param>
        /// <param name="data">Raw data to send.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task compeltion.</returns>
        Task SendDataAsync(
            SendOption sendOption,
            string[] targetParticipantIds,
            string type,
            byte[] data,
            CancellationToken cancellationToken);
    }
}
