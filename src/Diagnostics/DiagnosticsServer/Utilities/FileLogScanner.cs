// <copyright file="FileLogScanner.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticsServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Utilities
{
    /// <summary>
    /// The Log Scanner.
    /// </summary>
    public class FileLogScanner
    {
        private const int BundleSize = 10;

        private readonly IHubContext<LogHub> logHub;
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly CancellationToken stoppingToken;
        private readonly ILogHubSubscriptions hubSubscriptions;

        private DateTime currentLogFileDateTime;
        private string currentLogFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogScanner"/> class.
        /// </summary>
        /// <param name="logHub">The Log Hub.</param>
        /// <param name="hubSubscriptions">The hub subscriptions.</param>
        /// <param name="directory">Directory of the log file.</param>
        /// <param name="stoppingToken">The stopping token.</param>
        public FileLogScanner(IHubContext<LogHub> logHub, ILogHubSubscriptions hubSubscriptions, string directory, CancellationToken stoppingToken)
        {
            this.stoppingToken = stoppingToken;
            fileSystemWatcher = new FileSystemWatcher(directory);
            fileSystemWatcher.EnableRaisingEvents = true;
            fileSystemWatcher.Created += FileSystemWatcher_Created;
            this.logHub = logHub;
            this.hubSubscriptions = hubSubscriptions;
            this.Directory = directory;
            ScanForChanges();
        }

        /// <summary>
        /// Gets the current directory being scanned.
        /// </summary>
        public string Directory { get; private set; }

        /// <summary>
        /// Execute the worker actions.
        /// </summary>
        /// <returns>A task which will never complete.</returns>
        public async Task Execute()
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!string.IsNullOrEmpty(currentLogFile) && File.Exists(currentLogFile))
                {
                    using (FileStream stream = new FileStream(currentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader reader = new StreamReader(stream))
                    using (this.hubSubscriptions.OnReloadLogs(() =>
                    {
                        reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        return Task.CompletedTask;
                    }))
                    {
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            Thread.Sleep(100);

                            if (stream.Name != currentLogFile)
                            {
                                Debug.WriteLine($"({this.Directory}): finished current log and new log detected, changing to monitor {Path.GetFileName(currentLogFile)}");
                                break;
                            }

                            if (reader.EndOfStream)
                            {
                                continue;
                            }

                            List<string> bundle = new List<string>();
                            string line = string.Empty;
                            while ((line = reader.ReadLine()) != null)
                            {
                                try
                                {
                                    var jsonLine = line.Trim();
                                    if (jsonLine.StartsWith("{") && jsonLine.EndsWith("}"))
                                    {
                                        bundle.Add(jsonLine);
                                    }

                                    if (bundle.Count == BundleSize)
                                    {
                                        await logHub.Clients.All.SendAsync($"newJsonLogBundle", bundle);
                                        bundle.Clear();
                                    }
                                }
                                catch (System.Exception)
                                {
                                    await logHub.Clients.All.SendAsync($"newLogEntry", line);
                                }
                            }

                            if (bundle.Count > 0)
                            {
                                await logHub.Clients.All.SendAsync($"newJsonLogBundle", bundle);
                                bundle.Clear();
                            }
                        }
                    }
                }

                Thread.Sleep(1000);
                ScanForChanges();
            }
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            ScanForChanges();
        }

        private void ScanForChanges()
        {
            Debug.WriteLine($"({this.Directory}): Checking for files...");
            var directoryInfo = new DirectoryInfo(fileSystemWatcher.Path);
            foreach (var file in directoryInfo.EnumerateFiles().OrderBy(n => n.CreationTime))
            {
                try
                {
                    var date = DateTime.Parse(Path.GetFileNameWithoutExtension(file.Name).Replace(".", ":"));
                    if (date >= currentLogFileDateTime)
                    {
                        currentLogFileDateTime = date;
                        currentLogFile = file.FullName;
                        Debug.WriteLine($"({this.Directory}): log Found, {file.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);

                    // Ignore exceptions.
                }
            }
        }
    }
}
