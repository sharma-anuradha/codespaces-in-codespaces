// <copyright file="CacheSystemConfigurationWarmupTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// A task that will  hydrate the system configuration cache every minute.
    /// </summary>
    public class CacheSystemConfigurationWarmupTask : IAsyncWarmup
    {
        private const string LogBaseName = "cache_system_configuration_task";

        /// <summary>
        /// The cache refresh time interval in minutes
        /// </summary>
        private const int DefaultCacheRefreshTimeIntervalInMinutes = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheSystemConfigurationWarmupTask"/> class.
        /// </summary>
        /// <param name="taskHelper">The task helper for triggering background jobs.</param>
        /// <param name="healthProvider">The service health provider.</param>
        /// <param name="logger">logger.</param>
        public CacheSystemConfigurationWarmupTask(
            ITaskHelper taskHelper,
            ICachedSystemConfiguration cachedSystemConfiguration,
            IHealthProvider healthProvider,
            IDiagnosticsLogger logger)
        {
            TaskHelper = taskHelper;
            CachedSystemConfiguration = cachedSystemConfiguration;
            HealthProvider = healthProvider;
            DiagnosticsLogger = logger;
            InitializationTask = RefreshCacheAsync(logger);
        }

        private ITaskHelper TaskHelper { get; }

        private ICachedSystemConfiguration CachedSystemConfiguration { get; }

        private IHealthProvider HealthProvider { get; }

        private Task<bool> InitializationTask { get; }

        private IDiagnosticsLogger DiagnosticsLogger { get; }

        /// <inheritdoc/>
        public async Task WarmupCompletedAsync()
        {
            try
            {
                await InitializationTask;

                if (InitializationTask.Result != true)
                {
                    throw new InvalidOperationException($"{LogBaseName}_initialization_error");
                }

                // Kick off background job that hydrates the system configuration cache every minute
                await TaskHelper.RunBackgroundLoopAsync(
                    $"{LogBaseName}_run",
                    (childLogger) => RefreshCacheAsync(childLogger),
                    TimeSpan.FromMinutes(DefaultCacheRefreshTimeIntervalInMinutes));
            }
            catch (Exception ex)
            {
                HealthProvider.MarkUnhealthy(ex, DiagnosticsLogger);
            }            
        }

        private async Task<bool> RefreshCacheAsync(IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
               $"{LogBaseName}_run",
               async (childLogger) =>
               {
                   await CachedSystemConfiguration.RefreshCacheAsync(childLogger.NewChildLogger());
                   return true;
               }, swallowException: true);
        }
    }
}