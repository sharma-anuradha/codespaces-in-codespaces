// <copyright file="FileLogScanner.cs" company="Microsoft">
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

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Utilities
{
    /// <summary>
    /// The Log Scanner.
    /// </summary>
    public class FileLogScanner
    {
        private readonly IHubContext<LogHub> logHub;
        private readonly FileSystemWatcher fileSystemWatcher;

        private DateTime currentLogFileDateTime;
        private string currentLogFile;
        private string directory;

        private bool scanLog = false;
        private CancellationToken stoppingToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogScanner"/> class.
        /// </summary>
        /// <param name="logHub">The Log Hub.</param>
        /// <param name="directory">Directory of the log file.</param>
        /// <param name="stoppingToken">The stopping token.</param>
        public FileLogScanner(IHubContext<LogHub> logHub, string directory, CancellationToken stoppingToken)
        {
            this.stoppingToken = stoppingToken;
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            fileSystemWatcher = new FileSystemWatcher(directory);
            fileSystemWatcher.EnableRaisingEvents = true;
            fileSystemWatcher.Created += FileSystemWatcher_Created;
            this.logHub = logHub;
            this.directory = directory;
            ScanForChanges();
        }

        /// <summary>
        /// Execute the worker actions.
        /// </summary>
        public void Execute()
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!string.IsNullOrEmpty(currentLogFile) && File.Exists(currentLogFile))
                {
                    using (StreamReader reader = new StreamReader(new FileStream(currentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                    {
                        long lastMaxOffset = reader.BaseStream.Length;

                        while (scanLog && !stoppingToken.IsCancellationRequested)
                        {
                            Thread.Sleep(100);

                            if (reader.BaseStream.Length == lastMaxOffset)
                            {
                                continue;
                            }

                            reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);

                            string line = string.Empty;
                            while ((line = reader.ReadLine()) != null)
                            {
                                try
                                {
                                    var jsonLine = line.Trim();
                                    if (jsonLine.StartsWith("{") && jsonLine.EndsWith("}"))
                                    {
                                        logHub.Clients.All.SendAsync($"newJsonLogEntry", line);
                                    }
                                }
                                catch (System.Exception)
                                {
                                    logHub.Clients.All.SendAsync($"newLogEntry", line);
                                }
                            }

                            lastMaxOffset = reader.BaseStream.Position;
                        }
                    }
                }

                Thread.Sleep(1000);
                ScanForChanges();
            }
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            scanLog = false;
            ScanForChanges();
        }

        private void ScanForChanges()
        {
            Debug.WriteLine($"({this.directory}): Checking for files...");
            var directoryInfo = new DirectoryInfo(fileSystemWatcher.Path);
            var logUpdated = false;
            foreach (var file in directoryInfo.EnumerateFiles().OrderBy(n => n.CreationTime))
            {
                try
                {
                    var date = DateTime.Parse(Path.GetFileNameWithoutExtension(file.Name).Replace(".", ":"));
                    if (date >= currentLogFileDateTime)
                    {
                        currentLogFileDateTime = date;
                        currentLogFile = file.FullName;
                        logUpdated = true;
                        Debug.WriteLine($"({this.directory}): log Found, {file.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);

                    // Ignore exceptions.
                }
            }

            scanLog = logUpdated;
        }
    }
}
