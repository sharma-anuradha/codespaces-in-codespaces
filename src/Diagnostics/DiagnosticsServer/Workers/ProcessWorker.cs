// <copyright file="ProcessWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticsServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Workers
{
    /// <summary>
    /// The Process Worker.
    /// </summary>
    public class ProcessWorker : BackgroundService
    {
        private const string ProcessWorkerEvent = "processWorkerEvent";
        private readonly IHubContext<LogHub> logHub;
        private readonly List<string> processNames = new List<string>() { "ngrok", "Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApi", "Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi" };

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessWorker"/> class.
        /// </summary>
        /// <param name="logHub">The Log Hub.</param>
        public ProcessWorker(IHubContext<LogHub> logHub)
        {
            this.logHub = logHub;
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
                    foreach (var processName in processNames)
                    {
                        var process = System.Diagnostics.Process.GetProcessesByName(processName).FirstOrDefault();
                        if (process == null)
                        {
                            await logHub.Clients.All.SendAsync(ProcessWorkerEvent, new
                            {
                                type = EventType.Info,
                                process = processName,
                                isRunning = false,
                            });
                        }
                        else
                        {
                            var sessionTime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
                            await logHub.Clients.All.SendAsync(ProcessWorkerEvent, new
                            {
                                type = EventType.Info,
                                process = processName,
                                isRunning = true,
                                sessionTime = sessionTime.TotalMilliseconds,
                                startTime = process.StartTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    await logHub.Clients.All.SendAsync(ProcessWorkerEvent, new { type = EventType.Error, Exception = ex });
                }

                await Task.Delay(5000);
            }
        }
    }
}
