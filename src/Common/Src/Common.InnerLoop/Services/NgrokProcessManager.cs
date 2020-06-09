// <copyright file="NgrokProcessManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Services
{
    /// <summary>
    /// Creates an NgrokProcessManager.
    /// </summary>
    public class NgrokProcessManager
    {
        private readonly ILogger<NgrokProcessManager> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly IHostApplicationLifetime lifetime;

        private readonly NgrokOptions options;
        private readonly NgrokHttpClient apiClient;

        private readonly SemaphoreSlim processStartSemaphore = new SemaphoreSlim(0, 1);

        private NgrokProcess process;

        /// <summary>
        /// Initializes a new instance of the <see cref="NgrokProcessManager"/> class.
        /// </summary>
        /// <param name="loggerFactory">The Logger.</param>
        /// <param name="lifetime">The Lifetime.</param>
        /// <param name="optionsAccessor">The Options Accessor.</param>
        /// <param name="apiClient">The Ngrok API Client.</param>
        public NgrokProcessManager(
            ILoggerFactory loggerFactory,
            IHostApplicationLifetime lifetime,
            IOptionsMonitor<NgrokOptions> optionsAccessor,
            NgrokHttpClient apiClient)
        {
            logger = loggerFactory.CreateLogger<NgrokProcessManager>();
            this.loggerFactory = loggerFactory;
            options = optionsAccessor.CurrentValue;
            this.apiClient = apiClient;
            this.lifetime = lifetime;
        }

        /// <summary>
        /// Gets a value indicating whether Ngrok has started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Ensures Ngrok has started.
        /// </summary>
        /// <param name="cancellationToken">Notification that the Task should stop.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task EnsureNgrokStartedAsync(CancellationToken cancellationToken = default)
        {
            if (await apiClient.IsNgrokRunningAsync(cancellationToken))
            {
                var p = System.Diagnostics.Process.GetProcessesByName("ngrok").FirstOrDefault();
                if (p == null)
                {
                    throw new Exception("Should be a running Ngrok process found but couldn't find it.");
                }

                process = new NgrokProcess(p, lifetime, loggerFactory, options);
                return;
            }

            try
            {
                process = new NgrokProcess(lifetime, loggerFactory, options);

                // Register OnProcessStarted Handler
                process.ProcessStarted += OnProcessStarted;

                // Start Process
                process.StartNgrokProcess();

                // Wait for Process to be started
                await processStartSemaphore.WaitAsync(TimeSpan.FromMilliseconds(options.ProcessStartTimeoutMs), cancellationToken);

                // Verify API is up
                var isAPIUp = await apiClient.IsNgrokRunningAsync(cancellationToken);

                if (!isAPIUp)
                {
                    throw new Exception("Ngrok process has started, but couldn't valid API is up and running.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void OnProcessStarted()
        {
            IsStarted = true;
            processStartSemaphore.Release();
        }
    }
}
