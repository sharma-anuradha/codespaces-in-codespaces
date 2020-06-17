// <copyright file="FileLogWorker.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Utilities;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Workers
{
    /// <summary>
    /// The Log Worker.
    /// </summary>
    public class FileLogWorker
    {
        private readonly IHubContext<LogHub> logHub;
        private CancellationToken stoppingToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogWorker"/> class.
        /// </summary>
        /// <param name="logHub">The Log Hub.</param>
        /// <param name="applicationLifetime">The application lifetime.</param>
        public FileLogWorker(IHubContext<LogHub> logHub, IHostApplicationLifetime applicationLifetime)
        {
            this.logHub = logHub;
            stoppingToken = applicationLifetime.ApplicationStopping;
        }

        /// <summary>
        /// Execute the worker actions.
        /// </summary>
        public void Start()
        {
            foreach (var folder in Directory.GetDirectories(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"..\\..\\..\\logs")))
            {
                var logger = new FileLogScanner(this.logHub, folder, this.stoppingToken);
                Task.Run(() => logger.Execute());
            }
        }
    }
}