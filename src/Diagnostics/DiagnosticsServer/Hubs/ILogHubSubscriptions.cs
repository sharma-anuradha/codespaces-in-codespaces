// <copyright file="ILogHubSubscriptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

namespace DiagnosticsServer.Hubs
{
    /// <summary>
    /// Interface for subscriptions to <see cref="LogHub"/> events.
    /// </summary>
    public interface ILogHubSubscriptions
    {
        /// <summary>
        /// Subscribes a callback to <see cref="LogHub.ReloadLogs"/> events.
        /// </summary>
        /// <param name="action">The callback to be invoked.</param>
        /// <returns>An <see cref="IDisposable"/> which unsubscribes the callback when disposed.</returns>
        IDisposable OnReloadLogs(Func<Task> action);
    }
}