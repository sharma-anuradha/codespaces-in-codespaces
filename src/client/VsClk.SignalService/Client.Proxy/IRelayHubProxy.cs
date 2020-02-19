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
        /// When the hub is disconnected due to an unexpected loss of transport.
        /// </summary>
        event EventHandler Disconnected;

        /// <summary>
        /// When the hub is deleted
        /// </summary>
        event EventHandler Deleted;

        /// <summary>
        /// Gets the parent relay service proxy.
        /// </summary>
        IRelayServiceProxy RelayServiceProxy { get; }

        /// <summary>
        /// Gets the service id where this hub is hosted.
        /// </summary>
        string ServiceId { get; }

        /// <summary>
        /// Gets the stamp where this hub is hosted.
        /// </summary>
        string Stamp { get; }

        /// <summary>
        /// Gets unique id of the hub.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the self participant entity.
        /// </summary>
        IRelayHubParticipant SelfParticipant { get; }

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
        /// <returns>Task completion.</returns>
        Task SendDataAsync(
            SendOption sendOption,
            string[] targetParticipantIds,
            string type,
            byte[] data,
            CancellationToken cancellationToken);

        /// <summary>
        /// Update the joined participants properties.
        /// </summary>
        /// <param name="properties">new updated properties.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task compeltion.</returns>
        Task UpdateAsync(
                Dictionary<string, object> properties,
                CancellationToken cancellationToken);

        /// <summary>
        /// Rejoin a diposed or disconected hub proxy.
        /// </summary>
        /// <param name="joinOptions">Join options.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task completion.</returns>
        Task ReJoinAsync(JoinOptions joinOptions, CancellationToken cancellationToken);
    }
}
