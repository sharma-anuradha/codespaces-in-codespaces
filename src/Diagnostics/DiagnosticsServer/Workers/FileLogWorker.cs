// <copyright file="FileLogWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
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
        private readonly string logFolder;
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly IHubContext<LogHub> logHub;
        private CancellationToken stoppingToken;
        private List<FileLogScanner> scanners = new List<FileLogScanner>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogWorker"/> class.
        /// </summary>
        /// <param name="logHub">The Log Hub.</param>
        /// <param name="applicationLifetime">The application lifetime.</param>
        public FileLogWorker(IHubContext<LogHub> logHub, IHostApplicationLifetime applicationLifetime)
        {
            this.logHub = logHub;
            var assemblyFolder = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            this.logFolder = Path.Combine(assemblyFolder.Parent.Parent.Parent.FullName, "logs");
            Directory.CreateDirectory(this.logFolder);
            stoppingToken = applicationLifetime.ApplicationStopping;
            fileSystemWatcher = new FileSystemWatcher(this.logFolder);
            fileSystemWatcher.EnableRaisingEvents = true;
            fileSystemWatcher.Created += FileSystemWatcher_Event;
            fileSystemWatcher.Changed += FileSystemWatcher_Event;
        }

        /// <summary>
        /// Execute the worker actions.
        /// </summary>
        public void Start()
        {
            foreach (var folder in Directory.GetDirectories(this.logFolder))
            {
                if (this.scanners.Any(n => n.Directory == folder))
                {
                    continue;
                }

                Debug.WriteLine($"Directory Found: {folder}");
                var logger = new FileLogScanner(this.logHub, folder, this.stoppingToken);
                Task.Run(() => logger.Execute());
                this.scanners.Add(logger);
            }
        }

        private void FileSystemWatcher_Event(object sender, FileSystemEventArgs e)
        {
            this.Start();
        }
    }
}