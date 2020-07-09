// <copyright file="LogHub.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace DiagnosticsServer.Hubs
{
    /// <summary>
    /// SignalR Hub for logs.
    /// </summary>
    public class LogHub : Hub
    {
        private readonly ILogHubEventSource eventSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogHub"/> class.
        /// </summary>
        /// <param name="eventSource">The event source to forward client messages to.</param>
        public LogHub(ILogHubEventSource eventSource)
        {
            this.eventSource = eventSource;
        }

        /// <summary>
        /// Receives the "ReloadLogs" message from the client and invokes all subscribed server callbacks.
        /// </summary>
        /// <returns>A task for all subscribed callbacks to complete.</returns>
        public async Task ReloadLogs()
        {
            await this.eventSource.OnReloadLogs();
        }
    }
}
