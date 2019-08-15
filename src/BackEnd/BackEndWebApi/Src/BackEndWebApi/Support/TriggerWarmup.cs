// <copyright file="TriggerWarmup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApi.Support
{

    public class TriggerWarmup : ITriggerWarmup
    {
        public const string QueueName = "background-warmup-job-queue";

        /// <summary>
        /// Initializes a new instance of the <see cref="TriggerWarmup"/> class.
        /// </summary>
        /// <param name="healthProvider"></param>
        /// <param name="backgroundJobs"></param>
        /// <param name="asyncWarmupServices"></param>
        /// <param name="asyncBackgroundWarmupServices"></param>
        /// <param name="logger"></param>
        /// <param name="triggerWarmupState"></param>
        public TriggerWarmup(
             IHealthProvider healthProvider,
             IBackgroundJobClient backgroundJobs,
             IEnumerable<IAsyncWarmup> asyncWarmupServices,
             IEnumerable<IAsyncBackgroundWarmup> asyncBackgroundWarmupServices,
             IDiagnosticsLogger logger,
             TriggerWarmupState triggerWarmupState)
        {
            HealthProvider = healthProvider;
            BackgroundJobs = backgroundJobs;
            AsyncWarmupServices = asyncWarmupServices;
            AsyncBackgroundWarmupServices = asyncBackgroundWarmupServices;
            Logger = logger;
            EnqueuedState = new EnqueuedState
            {
                Queue = QueueName,
            };
            TriggerWarmupState = triggerWarmupState;
            WarmupTask = new Lazy<int>(
                () =>
                {
                    BackgroundJobs.Create(() => WarmupServices(), EnqueuedState);
                    return 1;
                });
        }


        private IHealthProvider HealthProvider { get; }

        private IBackgroundJobClient BackgroundJobs { get; }

        private IEnumerable<IAsyncWarmup> AsyncWarmupServices { get; }

        private IEnumerable<IAsyncBackgroundWarmup> AsyncBackgroundWarmupServices { get; }

        private IDiagnosticsLogger Logger;

        private IState EnqueuedState { get; }

        private Lazy<int> WarmupTask { get; set; }

        private TriggerWarmupState TriggerWarmupState { get; set; }

        /// <inheritdoc/>
        public int Start()
        {
            // Trigger task to run
            var result = WarmupTask.Value;

            return TriggerWarmupState.Status;
        }

        /// <summary>
        /// Needs to be public due to HangFire.
        /// </summary>
        /// <returns>Task to be executed.</returns>
        public async Task WarmupServices()
        {
            var duration = Logger.StartDuration();

            // Trigger normal service warmup
            await NormalWarmupServices(AsyncWarmupServices);

            if (HealthProvider.IsHealthy)
            {
                // Log the warmup success.
                Logger.AddDuration(duration).LogInfo("warmup");

                // Trigger delay service warmup
                DelayedWarmupServices(AsyncBackgroundWarmupServices);

                TriggerWarmupState.Status = StatusCodes.Status200OK;

                return;
            }

            // Log the warmup error.
            Logger.AddDuration(duration).LogError("warmup_error");
        }

        private async Task NormalWarmupServices(IEnumerable<IAsyncWarmup> services)
        {
            await WarmupUtility.WhenAllWarmupCompletedAsync(services);
        }

        private void DelayedWarmupServices(IEnumerable<IAsyncBackgroundWarmup> services)
        {
            foreach (var service in services)
            {
                try
                {
                    // Spawn out the tasks and run in parallel
                    var result = BackgroundJobs.Create(() => service.WarmupCompletedAsync(), EnqueuedState);
                }
                catch (Exception e)
                {
                    // Swallow warmup exceptions.
                    // These should be logged by the implementation.
                    // Failures should go to HealthProvider.MarkUnhealthy
                }
            }
        }
    }
}
