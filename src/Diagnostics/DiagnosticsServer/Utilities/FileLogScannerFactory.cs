// <copyright file="FileLogScannerFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using DiagnosticsServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Utilities
{
    /// <summary>
    /// Factory wrapper for <see cref="FileLogScanner"/>.
    /// </summary>
    public class FileLogScannerFactory
    {
        private readonly CancellationToken stoppingToken;

        private readonly IHubContext<LogHub> logHub;

        private readonly ILogHubSubscriptions hubSubscriptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileLogScannerFactory"/> class.
        /// </summary>
        /// <param name="logHub">The log hub.</param>
        /// <param name="hubSubscriptions">The log hub subscriptions.</param>
        /// <param name="applicationLifetime">The application lifetime.</param>
        public FileLogScannerFactory(IHubContext<LogHub> logHub, ILogHubSubscriptions hubSubscriptions, IHostApplicationLifetime applicationLifetime)
        {
            this.stoppingToken = applicationLifetime.ApplicationStopping;
            this.logHub = logHub;
            this.hubSubscriptions = hubSubscriptions;
        }

        /// <summary>
        /// Creates a new <see cref="FileLogScanner"/> for the given directory.
        /// </summary>
        /// <remarks>The returned scanner is not started, the caller should start it.</remarks>
        /// <param name="directory">The directory to monitor.</param>
        /// <returns>The scanner.</returns>
        public FileLogScanner New(string directory)
        {
            return new FileLogScanner(this.logHub, this.hubSubscriptions, directory, this.stoppingToken);
        }
    }
}