// <copyright file="NgrokWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticsServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Services;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Workers
{
    /// <summary>
    /// The Ngrok Worker.
    /// </summary>
    public class NgrokWorker : BackgroundService
    {
        private const string NgrokWorkerEvent = "newNgrokEvent";
        private readonly IHubContext<LogHub> logHub;
        private readonly NgrokHttpClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="NgrokWorker"/> class.
        /// </summary>
        /// <param name="logHub">The Log Hub.</param>
        public NgrokWorker(IHubContext<LogHub> logHub)
        {
            this.logHub = logHub;
            this.client = new NgrokHttpClient(new System.Net.Http.HttpClient());
        }

        /// <summary>
        /// Execute the worker actions.
        /// </summary>
        /// <param name="stoppingToken">The stopping token.</param>
        /// <returns>A task.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                try
                {
                    var ngrokProcess = System.Diagnostics.Process.GetProcessesByName("ngrok").FirstOrDefault();
                    if (ngrokProcess == null || await this.client.IsNgrokRunningAsync() == false)
                    {
                        await logHub.Clients.All.SendAsync(NgrokWorkerEvent, new NgrokEvent(EventType.Info, "isRunning", new { isRunning = false }));
                    }
                    else
                    {
                        // Ngrok is running.
                        var sessionTime = DateTime.UtcNow - ngrokProcess.StartTime.ToUniversalTime();
                        await logHub.Clients.All.SendAsync(NgrokWorkerEvent, new NgrokEvent(EventType.Info, "isRunning", new { isRunning = true, sessionTime = sessionTime.TotalMilliseconds }));

                        var tunnelsList = await this.client.ListTunnelsAsync();
                        await logHub.Clients.All.SendAsync(NgrokWorkerEvent, new NgrokEvent(EventType.Info, "tunnels", new { tunnels = tunnelsList }));

                        var capturedRequests = await this.client.ListCapturedRequestsAsync();
                        await logHub.Clients.All.SendAsync(NgrokWorkerEvent, new NgrokEvent(EventType.Info, "capturedRequests", new { requests = capturedRequests }));
                    }
                }
                catch (Exception ex)
                {
                    await logHub.Clients.All.SendAsync(NgrokWorkerEvent, new NgrokEvent(EventType.Error, "error", new { exception = ex }));
                }

                await Task.Delay(5000);
            }
        }
    }
}
