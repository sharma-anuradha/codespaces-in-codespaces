// <copyright file="ResourceRegisterJobs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Hangfire;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Registeres any jobs that need to be run on warmup.
    /// </summary>
    public class ResourceRegisterJobs : IAsyncBackgroundWarmup
    {
        /// <summary>
        /// Name of the queue being used.
        /// </summary>
        public const string QueueName = "recurring-queue";

        private const string Every5Minutes = "*/5 * * * *";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRegisterJobs"/> class.
        /// </summary>
        /// <param name="recurringJobManager">The job manager that runs the scheduled jobs.</param>
        /// <param name="watchPoolSizeJob">The <see cref="WatchPoolSizeJob"/> job that
        /// needs to be run.</param>
        public ResourceRegisterJobs(
            IRecurringJobManager recurringJobManager,
            WatchPoolSizeTask watchPoolSizeJob)
        {
            RecurringJobManager = recurringJobManager;
            WatchPoolSizeJob = watchPoolSizeJob;
        }

        private IRecurringJobManager RecurringJobManager { get; }

        private WatchPoolSizeTask WatchPoolSizeJob { get; }

        private RecurringJobOptions RecurringJobOptions { get; }

        /// <inheritdoc/>
        public Task WarmupCompletedAsync()
        {
            // Job: Watch Pool Size
            RecurringJobManager.AddOrUpdate(
                nameof(WatchPoolSizeJob),
                () => WatchPoolSizeJob.RunAsync(),
                Every5Minutes,
                null,
                QueueName);
            RecurringJobManager.Trigger(nameof(WatchPoolSizeJob));

            return Task.CompletedTask;
        }
    }
}
