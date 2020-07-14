// <copyright file="JobQueueConsumerFactoryTelemetry.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Telemetry background service for the IJobQueueConsumerFactory interface.
    /// </summary>
    public class JobQueueConsumerFactoryTelemetry : IAsyncBackgroundWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueueConsumerFactoryTelemetry"/> class.
        /// </summary>
        /// <param name="jobQueueConsumerFactory">The IJobQueueConsumerFactory instance.</param>
        /// <param name="taskHelper">Task helper to use.</param>
        public JobQueueConsumerFactoryTelemetry(
            IJobQueueConsumerFactory jobQueueConsumerFactory,
            ITaskHelper taskHelper)
        {
            JobQueueConsumerFactory = jobQueueConsumerFactory;
            TaskHelper = taskHelper;
        }

        private IJobQueueConsumerFactory JobQueueConsumerFactory { get; }

        private ITaskHelper TaskHelper { get; }

        /// <inheritdoc/>
        public Task BackgroundWarmupCompletedAsync(IDiagnosticsLogger logger)
        {
            TaskHelper.RunBackgroundLoop(
                "job_queue_consumer_factory_telemetry_run",
                async (childLogger) =>
                {
                    await childLogger.OperationScopeAsync(
                        "task_state",
                        async (childLogger2) =>
                        {
                            var metrics = JobQueueConsumerFactory.GetMetrics();
                            foreach (var kvp in metrics)
                            {
                                foreach (var jobPayloadHandlerMetricsKvp in kvp.Value)
                                {
                                    var jobPayloadHandlerMetrics = jobPayloadHandlerMetricsKvp.Value;
                                    await childLogger2.OperationScopeAsync(
                                        "task_state_item",
                                        (childLogger3) =>
                                        {
                                            childLogger3.FluentAddValue("Queue", kvp.Key)
                                                .FluentAddValue("JobPayloadHandlerType", jobPayloadHandlerMetricsKvp.Key)
                                                .FluentAddValue("TaskMinInputCount", jobPayloadHandlerMetrics.MinInputCount)
                                                .FluentAddValue("TaskMaxInputCount", jobPayloadHandlerMetrics.MaxInputCount)
                                                .FluentAddValue("TaskProcessed", jobPayloadHandlerMetrics.Processed)
                                                .FluentAddValue("TaskProcessTime", Math.Round(jobPayloadHandlerMetrics.ProcessTime.TotalMilliseconds / jobPayloadHandlerMetrics.Processed, 2).ToString())
                                                .FluentAddValue("TaskFailures", jobPayloadHandlerMetrics.Failures)
                                                .FluentAddValue("TaskRetries", jobPayloadHandlerMetrics.Retries)
                                                .FluentAddValue("TaskCancelled", jobPayloadHandlerMetrics.Cancelled);
                                            return Task.CompletedTask;
                                        });
                                }
                            }
                        },
                        swallowException: true);
                    return true;
                },
                TimeSpan.FromSeconds(60));
            return Task.CompletedTask;
        }
    }
}
