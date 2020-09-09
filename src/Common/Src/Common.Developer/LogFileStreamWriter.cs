// <copyright file="LogFileStreamWriter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Kusto.Ingest;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Developer
{
    /// <summary>
    /// Log File stream writer.
    /// </summary>
    public class LogFileStreamWriter : TextWriter
    {
        public LogFileStreamWriter()
        {
            var assemblyFolder = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            var directory = Path.Combine(assemblyFolder.Parent.Parent.Parent.FullName, "logs", $"{Assembly.GetEntryAssembly().GetName().Name}");
            var logDirectory = Path.GetFullPath(directory);
            var logFile = $"{DateTime.Now.ToString("s").Replace(":", ".")}.txt";
            var logFileDirectory = Path.Combine(logDirectory, logFile);
            Directory.CreateDirectory(logDirectory);
            Console.WriteLine($"Output redirected to Logs at {logFileDirectory}...");
            this.fileStream = new FileStream(logFileDirectory, FileMode.OpenOrCreate, FileAccess.Write);
            this.writer = new StreamWriter(this.fileStream);
            this.writer.AutoFlush = true;
        }

        /// <inheritdoc/>
        public override Encoding Encoding => Encoding.Default;

        /// <inheritdoc/>
        public override void WriteLine(string text)
        {
            lock (fileLock)
            {
                this.writer.WriteLine(text);
            }
        }

        public override void Close()
        {
            base.Close();
            this.writer.Close();
            this.fileStream.Close();
        }

        private FileStream fileStream;

        private StreamWriter writer;

        private readonly object fileLock = new object();
    }
}
