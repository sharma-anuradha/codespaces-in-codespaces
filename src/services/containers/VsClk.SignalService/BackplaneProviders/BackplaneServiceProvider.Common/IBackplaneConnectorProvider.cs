// <copyright file="IBackplaneConnectorProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Interface to define a generic connector provider to connecto a the backplane service.
    /// </summary>
    public interface IBackplaneConnectorProvider
    {
        /// <summary>
        /// When the connector get disconnected.
        /// </summary>
        event EventHandler Disconnected;

        /// <summary>
        /// Gets a value indicating whether gets the current state of the provider.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Add a method target delegate.
        /// </summary>
        /// <param name="methodName">Method name.</param>
        /// <param name="handler">Callback handler.</param>
        void AddTarget(string methodName, Delegate handler);

        /// <summary>
        /// Invoke a method on the remote side.
        /// </summary>
        /// <typeparam name="TResult">Type of result expected.</typeparam>
        /// <param name="targetName">Target method name.</param>
        /// <param name="arguments">Arguments to pass.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Result from the remote backplane.</returns>
        Task<TResult> InvokeAsync<TResult>(string targetName, object[] arguments, CancellationToken cancellationToken);

        /// <summary>
        /// Send data to the remote backplane.
        /// </summary>
        /// <param name="targetName">Target method name.</param>
        /// <param name="arguments">Arguments to pass.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Awaiting task.</returns>
        Task SendAsync(string targetName, object[] arguments, CancellationToken cancellationToken);

        /// <summary>
        /// Initiate an attempt to connect this provider.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Awaiting task.</returns>
        Task AttemptConnectAsync(CancellationToken cancellationToken);
    }
}
