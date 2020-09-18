// <copyright file="AzureBatchLoggerTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bond;
using Microsoft.Azure.Batch;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Tasks
{
    /// <summary>
    /// Task manager that regularly checks Azure Batch jobs and pools and records metrics.
    /// </summary>
    public class AzureBatchLoggerTask : BaseBackgroundTask, IAzureBatchLoggerTask
    {
        private const string LogBaseName = TaskConstants.AzureBatchLoggerTaskLogBaseName;
        private const string PrepareTaskDisplayName = TaskConstants.PrepareTaskDisplayName;
        private const string ArchiveTaskDisplayName = TaskConstants.ArchiveTaskDisplayName;
        private readonly string taskName = nameof(AzureBatchLoggerTask);

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBatchLoggerTask"/> class.
        /// </summary>
        /// <param name="taskHelper">The task helper.</param>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="batchClientFactory">Batch client factory.</param>
        /// <param name="storageProviderSettings">Storage provider settings.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        public AzureBatchLoggerTask(
            ITaskHelper taskHelper,
            IControlPlaneInfo controlPlaneInfo,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IBatchClientFactory batchClientFactory,
            StorageProviderSettings storageProviderSettings,
            IConfigurationReader configurationReader)
            : base(configurationReader)
        {
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            ClaimedDistributedLease = Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
            BatchClientFactory = Requires.NotNull(batchClientFactory, nameof(batchClientFactory));
            StorageProviderSettings = Requires.NotNull(storageProviderSettings, nameof(storageProviderSettings));
        }

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => "AzureBatchLoggerTask";

        private bool Disposed { get; set; }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{taskName}Lease");

        private ITaskHelper TaskHelper { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IBatchClientFactory BatchClientFactory { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private StorageProviderSettings StorageProviderSettings { get; }

        /// <inheritdoc/>
        protected override Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                (childLogger) =>
                {
                    var dataPlaneLocations = ControlPlaneInfo.Stamp.DataPlaneLocations;

                    // Run through data plane locations in the background
                    TaskHelper.RunBackgroundConcurrentEnumerable(
                        $"{LogBaseName}_run_dataplanelocation",
                        dataPlaneLocations,
                        (location, itemLogger) => RunOnDataPlaneLocationAsync(location, itemLogger),
                        childLogger,
                        (location, itemLogger) => ObtainLease($"{LeaseBaseName}-{location.ToString()}", claimSpan, itemLogger));

                    return Task.FromResult(!Disposed);
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            Disposed = true;
        }

        private async Task GetTaskTimes(string jobID, ODATADetailLevel taskQuery, List<double> runTimes, BatchClient batchClient)
        {
            var tasks = batchClient.JobOperations.ListTasks(jobID, taskQuery);
            await tasks.ForEachAsync(delegate(CloudTask task)
            {
                TimeSpan executionTime = (TimeSpan)(task.ExecutionInformation.EndTime - task.ExecutionInformation.StartTime);
                runTimes.Add(executionTime.TotalSeconds);
            });
        }

        public async Task<int> GetPoolStats(IDiagnosticsLogger logger, BatchClient batchClient)
        {
            var pools = batchClient.PoolOperations.ListPoolNodeCounts();
            await pools.ForEachAsync(delegate(PoolNodeCounts nodeCounts)
            {
                var poolLogger = logger.NewChildLogger();

                poolLogger.FluentAddValue("PoolId", nodeCounts.PoolId)
                .FluentAddValue("TotalDedicatedNodes", nodeCounts.Dedicated.Total)
                .FluentAddValue("RunningNodes", nodeCounts.Dedicated.Running)
                .FluentAddValue("IdleNodes", nodeCounts.Dedicated.Idle)
                .FluentAddValue("OfflineNodes", nodeCounts.Dedicated.Offline)
                .FluentAddValue("UnusableNodes", nodeCounts.Dedicated.Unusable)
                .FluentAddValue("UnknownNodes", nodeCounts.Dedicated.Unknown)
                .FluentAddValue("StartingNodes", nodeCounts.Dedicated.Starting)
                .FluentAddValue("StoppingNodes", nodeCounts.Dedicated.LeavingPool)
                .LogInfo($"{LogBaseName}_pool_status");
            });
            return pools.Count();
        }

        public async void LogJobStats(IDiagnosticsLogger logger, CloudJob job, BatchClient batchClient, ODATADetailLevel taskQuery, TaskCounts taskCounts)
        {
            List<double> taskTimes = new List<double>();
            var displayName = job.DisplayName;
            logger.FluentAddValue("ActiveTasks", taskCounts.Active)
                    .FluentAddValue("RunningTasks", taskCounts.Running)
                    .FluentAddValue("CompletedTasks", taskCounts.Completed)
                    .FluentAddValue("SucceededTasks", taskCounts.Succeeded)
                    .FluentAddValue("FailedTasks", taskCounts.Failed)
                    .FluentAddValue("DisplayName", job.DisplayName)
                    .FluentAddValue("JobId", job.Id);

            if (taskCounts.Completed > 0)
            {
                logger.FluentAddValue("SuccessRate", taskCounts.Succeeded / taskCounts.Completed);
            }

            await GetTaskTimes(job.Id, taskQuery, taskTimes, batchClient);

            if (displayName.Equals(ArchiveTaskDisplayName))
            {
                logger.FluentAddValue("TaskType", "Archive");
            }
            else if (displayName.Equals(PrepareTaskDisplayName))
            {
                logger.FluentAddValue("TaskType", "Prepare");
            }

            if (taskTimes.Count > 0)
            {
                logger.FluentAddValue("AverageExecutionTimeSec", taskTimes.Average())
                      .FluentAddValue("MinExecutionTimeSec", taskTimes.Min())
                      .FluentAddValue("MaxExecutionTimeSec", taskTimes.Max());
            }

            logger.LogInfo($"{LogBaseName}_job_status");
        }

        /// <summary>
        /// Gets info on jobs and pools and writes to logger
        /// </summary>
        /// <param name="location">Azure Location.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>Task.</returns>
        private async Task RunOnDataPlaneLocationAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            using (var batchClient = await BatchClientFactory.GetBatchClient(location, logger))
            {
                List<double> archiveTimes = new List<double>();
                List<double> prepareTimes = new List<double>();
                int activeTasks = 0;
                int runningTasks = 0;
                int completedTasks = 0;
                int succeededTasks = 0;
                int failedTasks = 0;
                var finishedAfter = DateTime.UtcNow.AddMinutes(-30);
                var jobsOdataQuery = new ODATADetailLevel(
                    filterClause: $"executionInfo/poolId eq '{StorageProviderSettings.WorkerBatchPoolId}' and state eq 'active'",
                    selectClause: "id,displayName");
                var taskOdataQuery = new ODATADetailLevel(
                    filterClause: $"executionInfo/endTime ge DateTime'{finishedAfter:o}' and state eq 'Completed' and executionInfo/result eq 'Success'");

                var jobs = batchClient.JobOperations.ListJobs(jobsOdataQuery);
                await jobs.ForEachAsync(async delegate(CloudJob job)
                {
                    var displayName = job.DisplayName;
                    TaskCounts taskCounts = await batchClient.JobOperations.GetJobTaskCountsAsync(job.Id);
                    LogJobStats(logger.NewChildLogger(), job, batchClient, taskOdataQuery, taskCounts);
                    activeTasks += taskCounts.Active;
                    runningTasks += taskCounts.Running;
                    completedTasks += taskCounts.Completed;
                    succeededTasks += taskCounts.Succeeded;
                    failedTasks += taskCounts.Failed;

                    if (displayName.Equals(ArchiveTaskDisplayName))
                    {
                        await GetTaskTimes(job.Id, taskOdataQuery, archiveTimes, batchClient);
                    }
                    else if (displayName.Equals(PrepareTaskDisplayName))
                    {
                       await GetTaskTimes(job.Id, taskOdataQuery, prepareTimes, batchClient);
                    }
                });

                logger.FluentAddValue("ActiveTasks", activeTasks)
                    .FluentAddValue("RunningTasks", runningTasks)
                    .FluentAddValue("CompletedTasks", completedTasks)
                    .FluentAddValue("SucceededTasks", succeededTasks)
                    .FluentAddValue("FailedTasks", failedTasks);

                if (completedTasks > 0)
                {
                    logger.FluentAddValue("SuccessRate", succeededTasks / completedTasks);
                }

                if (archiveTimes.Count > 0)
                {
                    logger.FluentAddValue("AverageArchiveExecutionTimeSec", archiveTimes.Average())
                        .FluentAddValue("MinArchiveExecutionTimeSec", archiveTimes.Min())
                        .FluentAddValue("MaxArchiveExecutionTimeSec", archiveTimes.Max());
                }

                if (prepareTimes.Count > 0)
                {
                    logger.FluentAddValue("AveragePrepareExecutionTimeSec", prepareTimes.Average())
                         .FluentAddValue("MinPrepareExecutionTimeSec", prepareTimes.Min())
                         .FluentAddValue("MaxPrepareExecutionTimeSec", prepareTimes.Max());
                }

                logger.FluentAddValue("NumberOfJobs", jobs.Count());
                int numPools = await GetPoolStats(logger, batchClient);
                logger.FluentAddValue("NumberOfPools", numPools);
                logger.LogInfo($"{LogBaseName}_overall_status");
            }
        }

        private async Task<IDisposable> ObtainLease(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return await ClaimedDistributedLease.Obtain(
                TaskConstants.LeaseContainerName, leaseName, claimSpan, logger);
        }
    }
}
