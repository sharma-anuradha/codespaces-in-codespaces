// <copyright file="WarmupController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackendWebApi.Controllers
{
    /// <summary>
    /// This controller is used to warmup dependent service connections.
    /// We use this to prevent traffic from hitting the service until everything is ready.
    /// </summary>
    [ApiController]
    [Route("warmup")]
    public class WarmupController : ControllerBase
    {
        public const string QueueName = "background-warmup-job-queue";

        /// <summary>
        /// Initializes a new instance of the <see cref="WarmupController"/> class.
        /// </summary>
        /// <param name="healthProvider"></param>
        /// <param name="backgroundJobs"></param>
        /// <param name="asyncWarmupServices"></param>
        /// <param name="asyncBackgroundWarmupServices"></param>
        public WarmupController(
             IHealthProvider healthProvider,
             IBackgroundJobClient backgroundJobs,
             IEnumerable<IAsyncWarmup> asyncWarmupServices,
             IEnumerable<IAsyncBackgroundWarmup> asyncBackgroundWarmupServices)
        {
            HealthProvider = healthProvider;
            BackgroundJobs = backgroundJobs;
            AsyncWarmupServices = asyncWarmupServices;
            AsyncBackgroundWarmupServices = asyncBackgroundWarmupServices;
            EnqueuedState = new EnqueuedState
            {
                Queue = QueueName,
            };
        }

        private IHealthProvider HealthProvider { get; }

        private IBackgroundJobClient BackgroundJobs { get; }

        private IEnumerable<IAsyncWarmup> AsyncWarmupServices { get; }

        private IEnumerable<IAsyncBackgroundWarmup> AsyncBackgroundWarmupServices { get; }

        private IState EnqueuedState { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAsync()
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            // Trigger normal service warmup
            await TriggerNormalWarmupServices(AsyncWarmupServices);

            if (HealthProvider.IsHealthy)
            {
                // Log the warmup success.
                logger.AddDuration(duration).LogInfo("warmup");

                // Trigger delay service warmup
                TriggerDelayedWarmupServices(AsyncBackgroundWarmupServices);

                return Ok();
            }

            // Log the warmup error.
            logger.AddDuration(duration).LogError("warmup_error");

            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        private async Task TriggerNormalWarmupServices(IEnumerable<IAsyncWarmup> services)
        {
            await WarmupUtility.WhenAllWarmupCompletedAsync(services);
        }

        private void TriggerDelayedWarmupServices(IEnumerable<IAsyncBackgroundWarmup> services)
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
