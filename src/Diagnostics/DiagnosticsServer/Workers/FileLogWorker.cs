// <copyright file="FileLogWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DiagnosticsServer.Hubs;
using Microsoft.AspNetCore.SignalR;
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
        private readonly object logLock = new object();
        private readonly FileLogScannerFactory fileLogScannerFactory;

        private List<FileLogScanner> scanners = new List<FileLogScanner>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogWorker"/> class.
        /// </summary>
        /// <param name="scannerFactory">The scanner factory.</param>
        public FileLogWorker(FileLogScannerFactory scannerFactory)
        {
            var assemblyFolder = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            this.logFolder = Path.Combine(assemblyFolder.Parent.Parent.Parent.FullName, "logs");
            Directory.CreateDirectory(this.logFolder);
            fileSystemWatcher = new FileSystemWatcher(this.logFolder);
            fileSystemWatcher.EnableRaisingEvents = true;
            fileSystemWatcher.Created += FileSystemWatcher_Event;
            fileSystemWatcher.Changed += FileSystemWatcher_Event;

            this.fileLogScannerFactory = scannerFactory;
        }

        /// <summary>
        /// Execute the worker actions.
        /// </summary>
        public void Start()
        {
            lock (this.logLock)
            {
                foreach (var folder in Directory.GetDirectories(this.logFolder))
                {
                    if (this.scanners.ToList().Any(n => n.Directory == folder))
                    {
                        continue;
                    }

                    Debug.WriteLine($"Directory Found: {folder}");
                    var logger = this.fileLogScannerFactory.New(folder);
                    Task.Run(() => logger.Execute());
                    this.scanners.Add(logger);
                }
            }
        }

        private void FileSystemWatcher_Event(object sender, FileSystemEventArgs e)
        {
            this.Start();
        }
    }
}