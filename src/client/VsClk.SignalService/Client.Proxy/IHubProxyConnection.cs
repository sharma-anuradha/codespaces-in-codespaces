// <copyright file="IHubProxyConnection.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// A hub proxy connection contract.
    /// </summary>
    public interface IHubProxyConnection
    {
        /// <summary>
        /// When a connection state changed.
        /// </summary>
        event AsyncEventHandler ConnectionStateChanged;

        /// <summary>
        /// Gets a value indicating whether if the hub is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Ensure the hub is being connected.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task result.</returns>
        Task ConnectAsync(CancellationToken cancellationToken);
    }
}
