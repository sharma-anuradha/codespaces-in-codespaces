// <copyright file="IHubClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// The hub client interface.
    /// </summary>
    public interface IHubClient : IAsyncDisposable, IHubProxyConnection
    {
        /// <summary>
        /// When an attempt to connect is being done.
        /// </summary>
        event AsyncEventHandler<AttemptConnectionEventArgs> AttemptConnection;

        /// <summary>
        /// Gets the current hub connection state.
        /// </summary>
        HubConnectionState State { get; }

        /// <summary>
        /// Gets a value indicating whether if the hub connection is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Start the hub conenction.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task result.</returns>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stop the hub connection.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task result.</returns>
        Task StopAsync(CancellationToken cancellationToken);
    }
}
