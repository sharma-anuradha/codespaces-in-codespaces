// <copyright file="WatchStorageAzureBatchCleanupTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Tasks
{
    /// <summary>
    /// Task manager that regularly checks Azure Batch jobs and cleans them up as needed.
    /// </summary>
    public class WatchStorageAzureBatchCleanupTask : IWatchStorageAzureBatchCleanupTask
    {
        private const string LogBaseName = TaskConstants.WatchStorageAzureBatchCleanupTaskLogBaseName;
        private readonly string taskName = nameof(WatchStorageAzureBatchCleanupTask);

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchStorageAzureBatchCleanupTask"/> class.
        /// </summary>
        /// <param name="taskHelper">The task helper.</param>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="batchClientFactory">Batch client factory.</param>
        /// <param name="storageProviderSettings">Storage provider settings.</param>
        public WatchStorageAzureBatchCleanupTask(
            ITaskHelper taskHelper,
            IControlPlaneInfo controlPlaneInfo,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IBatchClientFactory batchClientFactory,
            StorageProviderSettings storageProviderSettings)
        {
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            ClaimedDistributedLease = Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
            BatchClientFactory = Requires.NotNull(batchClientFactory, nameof(batchClientFactory));
            StorageProviderSettings = Requires.NotNull(storageProviderSettings, nameof(storageProviderSettings));
        }

        private bool Disposed { get; set; }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{taskName}Lease");

        private ITaskHelper TaskHelper { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IBatchClientFactory BatchClientFactory { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private StorageProviderSettings StorageProviderSettings { get; }

        /// <inheritdoc/>
        public Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    var dataPlaneLocations = ControlPlaneInfo.Stamp.DataPlaneLocations;

                    // Run through data plane locations in the background
                    TaskHelper.RunBackgroundEnumerable(
                        $"{LogBaseName}_run_dataplanelocation",
                        dataPlaneLocations,
                        (location, itemLogger) => RunOnDataPlaneLocationAsync(location, itemLogger),
                        childLogger,
                        (location, itemLogger) => ObtainLease($"{LeaseBaseName}-{location.ToString()}", claimSpan, itemLogger));

                    return !Disposed;
                },
                (e, childLogger) => !Disposed,
                swallowException: true);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        /// <summary>
        /// Deletes stale jobs (jobs that haven't had a task scheduled in the last 1 hour).
        /// We will only delete these jobs if there is in fact at least 1 other job that isn't stale.
        /// This ensures that we don't delete any jobs that just haven't had any recent activity if there isn't a recently used job available.
        /// </summary>
        /// <param name="location">Azure Location.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>Task.</returns>
        private async Task RunOnDataPlaneLocationAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            using (BatchClient batchClient = await BatchClientFactory.GetBatchClient(location, logger))
            {
                var createdAfter = DateTime.UtcNow.AddHours(-1);
                var jobsOdataQuery = new ODATADetailLevel(
                    filterClause: $"executionInfo/poolId eq '{StorageProviderSettings.WorkerBatchPoolId}' and state eq 'active'",
                    selectClause: "id");
                var tasksOdataQuery = new ODATADetailLevel(
                    filterClause: $"creationTime ge DateTime'{createdAfter:o}'",
                    selectClause: "id");

                logger.FluentAddValue("AzureLocation", location.ToString())
                    .FluentAddValue("JobsQuery", jobsOdataQuery.FilterClause)
                    .FluentAddValue("JobTasksQuery", tasksOdataQuery.FilterClause);

                var jobs = await batchClient.JobOperations.ListJobs(jobsOdataQuery).ToListAsync();

                logger.FluentAddValue("TotalJobsCount", jobs.Count());
                logger.FluentAddValue("TotalJobIds", $"[{string.Join(",", jobs.Select(j => j.Id))}]");

                var entries = jobs.Select(job => (JobId: job.Id, HasRecentTasks: batchClient.JobOperations.ListTasks(job.Id, tasksOdataQuery).Any()));
                var groupedJobIds = entries.GroupBy(e => e.HasRecentTasks).ToDictionary(grouping => grouping.Key, grouping => grouping.Select(item => item.JobId));

                groupedJobIds.TryGetValue(false, out var possibleStaleJobIds);
                groupedJobIds.TryGetValue(true, out var recentlyUsedJobIds);

                var hasStaleJobs = possibleStaleJobIds != null && possibleStaleJobIds.Count() > 0
                    && recentlyUsedJobIds != null && recentlyUsedJobIds.Count() > 0;

                logger.FluentAddValue("PossibleStaleJobsCount", possibleStaleJobIds?.Count())
                    .FluentAddValue("RecentlyUsedJobsCount", recentlyUsedJobIds?.Count())
                    .FluentAddValue("PossibleStaleJobIds", possibleStaleJobIds != null ? $"[{string.Join(",", possibleStaleJobIds)}]" : string.Empty)
                    .FluentAddValue("RecentlyUsedJobIds", recentlyUsedJobIds != null ? $"[{string.Join(",", recentlyUsedJobIds)}]" : string.Empty)
                    .FluentAddValue("PossibleStaleJobsAreStale", hasStaleJobs);

                if (hasStaleJobs)
                {
                    await Task.WhenAll(possibleStaleJobIds.Select(jobId => batchClient.JobOperations.DeleteJobAsync(jobId)));
                }
            }
        }

        private async Task<IDisposable> ObtainLease(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return await ClaimedDistributedLease.Obtain(
                TaskConstants.LeaseContainerName, leaseName, claimSpan, logger);
        }
    }
}
