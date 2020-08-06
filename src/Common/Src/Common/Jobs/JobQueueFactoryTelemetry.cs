// <copyright file="JobQueueFactoryTelemetry.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class JobQueueFactoryTelemetry : IAsyncBackgroundWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueueFactoryTelemetry"/> class.
        /// </summary>
        /// <param name="jobQueueConsumerFactory">The IJobQueueConsumerFactory instance.</param>
        /// <param name="jobQueueProducerFactory">The IJobQueueProducerFactory instance.</param>
        /// <param name="taskHelper">Task helper to use.</param>
        public JobQueueFactoryTelemetry(
            IJobQueueConsumerFactory jobQueueConsumerFactory,
            IJobQueueProducerFactory jobQueueProducerFactory,
            ITaskHelper taskHelper)
        {
            JobQueueConsumerFactory = Requires.NotNull(jobQueueConsumerFactory, nameof(jobQueueConsumerFactory));
            JobQueueProducerFactory = Requires.NotNull(jobQueueProducerFactory, nameof(jobQueueProducerFactory));
            TaskHelper = taskHelper;
        }

        private IJobQueueConsumerFactory JobQueueConsumerFactory { get; }

        private IJobQueueProducerFactory JobQueueProducerFactory { get; }

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
                            // Produce metrics for IJobQueueConsumerFactory
                            var jobHandlerConsumerMetrics = JobQueueConsumerFactory.GetMetrics();
                            foreach (var kvp in jobHandlerConsumerMetrics)
                            {
                                foreach (var jobPayloadHandlerMetricsKvp in kvp.Value)
                                {
                                    var jobPayloadHandlerMetrics = jobPayloadHandlerMetricsKvp.Value;
                                    var jobProcessTimes = jobPayloadHandlerMetrics.ProcessTimes.Select(t => t.TotalMilliseconds);

                                    await childLogger2.OperationScopeAsync(
                                        "job_queue_consumer_summary_item",
                                        (childLogger3) =>
                                        {
                                            childLogger3.FluentAddValue("Queue", kvp.Key)
                                                .FluentAddValue(JobQueueLoggerConst.JobType, jobPayloadHandlerMetricsKvp.Key)
                                                .FluentAddValue(JobQueueLoggerConst.JobQueueMinInputCount, jobPayloadHandlerMetrics.MinInputCount)
                                                .FluentAddValue(JobQueueLoggerConst.JobQueueMaxInputCount, jobPayloadHandlerMetrics.MaxInputCount)
                                                .FluentAddValue(JobQueueLoggerConst.JobProcessedCount, jobPayloadHandlerMetrics.Processed)
                                                .FluentAddValue(JobQueueLoggerConst.JobAverageProcessTime, jobPayloadHandlerMetrics.Processed == 0 ? string.Empty : Math.Round(jobPayloadHandlerMetrics.ProcessTime.TotalMilliseconds / jobPayloadHandlerMetrics.Processed, 2).ToString())
                                                .FluentAddValue(JobQueueLoggerConst.JobFailuresCount, jobPayloadHandlerMetrics.Failures)
                                                .FluentAddValue(JobQueueLoggerConst.JobRetriesCount, jobPayloadHandlerMetrics.Retries)
                                                .FluentAddValue(JobQueueLoggerConst.JobCancelledCount, jobPayloadHandlerMetrics.Cancelled)
                                                .FluentAddValue(JobQueueLoggerConst.JobExpiredCount, jobPayloadHandlerMetrics.Expired)
                                                .FluentAddValue(JobQueueLoggerConst.JobKeepInvisibleCount, jobPayloadHandlerMetrics.KeepInvisibleCount)
                                                .FluentAddValue(JobQueueLoggerConst.JobPercentile50Time, GetPercentile(jobProcessTimes, 0.5))
                                                .FluentAddValue(JobQueueLoggerConst.JobPercentile90Time, GetPercentile(jobProcessTimes, 0.9))
                                                .FluentAddValue(JobQueueLoggerConst.JobPercentile99Time, GetPercentile(jobProcessTimes, 0.99));
                                            return Task.CompletedTask;
                                        });
                                }
                            }

                            // Produce metrics for IJobQueueProducerFactory
                            var jobQueueProducerFactoryMetrics = JobQueueProducerFactory.GetMetrics();
                            foreach (var kvp in jobQueueProducerFactoryMetrics)
                            {
                                foreach (var jobQueueProducerMetricsKvp in kvp.Value)
                                {
                                    var jobQueueProducerMetrics = jobQueueProducerMetricsKvp.Value;
                                    await childLogger2.OperationScopeAsync(
                                        "job_queue_factory_summary_item",
                                        (childLogger3) =>
                                        {
                                            childLogger3.FluentAddValue("Queue", kvp.Key)
                                                .FluentAddValue(JobQueueLoggerConst.JobType, jobQueueProducerMetricsKvp.Key)
                                                .FluentAddValue(JobQueueLoggerConst.JobProcessedCount, jobQueueProducerMetrics.Processed)
                                                .FluentAddValue(JobQueueLoggerConst.JobFailuresCount, jobQueueProducerMetrics.Failures);
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

        private static double GetPercentile(IEnumerable<double> seq, double percentile)
        {
            var elements = seq.ToArray();
            if (elements.Length == 0)
            {
                return 0;
            }

            Array.Sort(elements);
            double realIndex = percentile * (elements.Length - 1);
            int index = (int)realIndex;
            double frac = realIndex - index;
            if (index + 1 < elements.Length)
            {
                return Math.Round((elements[index] * (1 - frac)) + (elements[index + 1] * frac), 2);
            }
            else
            {
                return Math.Round(elements[index], 2);
            }
        }
    }
}
