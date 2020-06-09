// <copyright file="NgrokProcess.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Services
{
    /// <summary>
    /// Managing an Ngrok Process.
    /// </summary>
    public class NgrokProcess
    {
        private readonly NgrokOptions options;
        private Process process;
        private ILogger ngrokProcessLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NgrokProcess"/> class.
        /// </summary>
        /// <param name="applicationLifetime">The Application Lifetime.</param>
        /// <param name="loggerFactory">The Logger Factory.</param>
        /// <param name="options">The Options.</param>
        public NgrokProcess(
            IHostApplicationLifetime applicationLifetime,
            ILoggerFactory loggerFactory,
            NgrokOptions options)
        {
            ngrokProcessLogger = loggerFactory.CreateLogger("NgrokProcess");
            this.options = options;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NgrokProcess"/> class.
        /// </summary>
        /// <param name="process">A running ngrok process.</param>
        /// <param name="applicationLifetime">The Application Lifetime.</param>
        /// <param name="loggerFactory">The Logger Factory.</param>
        /// <param name="options">The Options.</param>
        public NgrokProcess(
            Process process,
            IHostApplicationLifetime applicationLifetime,
            ILoggerFactory loggerFactory,
            NgrokOptions options)
        {
            this.process = process;
            ngrokProcessLogger = loggerFactory.CreateLogger("NgrokProcess");
            this.options = options;
        }

        /// <summary>
        /// Gets or sets an action stating if the process has started.
        /// </summary>
        public Action ProcessStarted { get; set; }

        /// <summary>
        /// Start Ngrok Session.
        /// </summary>
        public void StartNgrokProcess()
        {
            var linuxProcessStartInfo = new ProcessStartInfo("/bin/bash", "-c \"ngrok start --none\"")
            {
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
            };

            var windowsProcessStartInfo = new ProcessStartInfo("Ngrok.exe", "start --none")
            {
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
            };

            var processInformation = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                windowsProcessStartInfo :
                linuxProcessStartInfo;

            Start(processInformation);
        }

        /// <summary>
        /// Start Ngrok Session.
        /// </summary>
        /// <param name="processStartInfo">The ProcessStartInfo.</param>
        protected virtual void Start(ProcessStartInfo processStartInfo)
        {
            var process = new Process();
            process.StartInfo = processStartInfo;

            process.Start();

            this.process = process;
        }
    }
}
