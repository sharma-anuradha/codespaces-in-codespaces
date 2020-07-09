// <copyright file="ILogHubEventSource.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace DiagnosticsServer.Hubs
{
    /// <summary>
    /// Interface for sending <see cref="LogHub"/> events to subscribers.
    /// </summary>
    public interface ILogHubEventSource
    {
        /// <summary>
        /// Invokes subscribed callbacks for <see cref="LogHub.ReloadLogs"/> events.
        /// </summary>
        /// <returns>A task which resolves when all callbacks complete.</returns>
        Task OnReloadLogs();
    }
}