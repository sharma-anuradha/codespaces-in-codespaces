// <copyright file="TriggerWarmup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Warmup
{
    /// <summary>
    /// Task that manages the initial warmup of the site.
    /// </summary>
    public class TriggerWarmup : ITriggerWarmup
    {
        private const string LogBaseName = "site-warmup";

        /// <summary>
        /// Initializes a new instance of the <see cref="TriggerWarmup"/> class.
        /// </summary>
        /// <param name="healthProvider">Health provider.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="asyncWarmupServices">List of warmup services.</param>
        /// <param name="asyncBackgroundWarmupServices">List of background warmup services.</param>
        public TriggerWarmup(
             IHealthProvider healthProvider,
             ITaskHelper taskHelper,
             IEnumerable<IAsyncWarmup> asyncWarmupServices,
             IEnumerable<IAsyncBackgroundWarmup> asyncBackgroundWarmupServices)
        {
            HealthProvider = healthProvider;
            TaskHelper = taskHelper;
            AsyncWarmupServices = asyncWarmupServices;
            AsyncBackgroundWarmupServices = asyncBackgroundWarmupServices;
            TriggerWarmupState = StatusCodes.Status503ServiceUnavailable;
            WarmupTask = new Lazy<int>(
                () =>
                {
                    TaskHelper.RunBackground(
                        LogBaseName,
                        (childLogger) => WarmupServicesAsync(childLogger));

                    return 1;
                });
        }

        private IHealthProvider HealthProvider { get; }

        private ITaskHelper TaskHelper { get; }

        private IEnumerable<IAsyncWarmup> AsyncWarmupServices { get; }

        private IEnumerable<IAsyncBackgroundWarmup> AsyncBackgroundWarmupServices { get; }

        private Lazy<int> WarmupTask { get; set; }

        private int TriggerWarmupState { get; set; }

        /// <inheritdoc/>
        public int Start()
        {
            // Trigger task to run
            var result = WarmupTask.Value;

            return TriggerWarmupState;
        }

        private async Task WarmupServicesAsync(IDiagnosticsLogger logger)
        {
            // Trigger normal service warmup
            await NormalWarmupServices(AsyncWarmupServices);

            if (HealthProvider.IsHealthy)
            {
                // Trigger delay service warmup
                DelayedWarmupServices(AsyncBackgroundWarmupServices, logger);

                TriggerWarmupState = StatusCodes.Status200OK;

                return;
            }

            throw new Exception("Warmup failed to complete successfully.");
        }

        private async Task NormalWarmupServices(IEnumerable<IAsyncWarmup> services)
        {
            await WarmupUtility.WhenAllWarmupCompletedAsync(services);
        }

        private void DelayedWarmupServices(IEnumerable<IAsyncBackgroundWarmup> services, IDiagnosticsLogger logger)
        {
            foreach (var service in services)
            {
                // Spawn out the tasks and run in parallel
                TaskHelper.RunBackground(
                    $"{LogBaseName}_delayed_warmup",
                    (childLogger) =>
                    {
                        childLogger.FluentAddValue("ServiceName", service.GetType().Name);

                        return service.BackgroundWarmupCompletedAsync(childLogger);
                    },
                    logger);
            }
        }
    }
}
