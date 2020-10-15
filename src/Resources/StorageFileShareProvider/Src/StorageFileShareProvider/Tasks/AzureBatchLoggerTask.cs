// <copyright file="AzureBatchLoggerTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bond;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;
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

        /// <summary>
        /// Get run times for tasks in a job
        /// </summary>
        /// <param name="jobID">Job ID</param>
        /// <param name="taskQuery">Query to get specific task</param>
        /// <param name="runTimes">Array to hold result</param>
        /// <param name="batchClient">Batch Client</param>
        /// <returns>List of Tasks inside Job</returns>
        private async Task<double?> GetTaskTimes(string jobID, ODATADetailLevel taskQuery, List<double> runTimes, BatchClient batchClient)
        {
            var succeededTasks = 0;
            var failedTasks = 0;
            var tasks = batchClient.JobOperations.ListTasks(jobID, taskQuery);
            await tasks.ForEachAsync(delegate(CloudTask task)
            {
                if (task.ExecutionInformation.Result == TaskExecutionResult.Success)
                {
                    TimeSpan executionTime = (TimeSpan)(task.ExecutionInformation.EndTime - task.ExecutionInformation.StartTime);
                    runTimes.Add(executionTime.TotalSeconds);
                    Interlocked.Increment(ref succeededTasks);
                }
                else
                {
                    Interlocked.Increment(ref failedTasks);
                }
            });
            if (succeededTasks + failedTasks > 0)
            {
                return (succeededTasks * 100 / (double)(succeededTasks + failedTasks));
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get Statistics on pool and write to logger
        /// </summary>
        /// <param name="logger">Logger used to create child loggers</param>
        /// <param name="batchClient">Batch client</param>
        /// <returns>Number of Pools</returns>
        public async Task<int> GetPoolStats(IDiagnosticsLogger logger, BatchClient batchClient)
        {
            var pools = batchClient.PoolOperations.ListPoolNodeCounts();
            await pools.ForEachAsync(delegate(PoolNodeCounts nodeCounts)
            {
                logger.OperationScopeAsync(
                    $"{LogBaseName}_pool_status",
                    (childLogger) =>
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
                        return Task.FromResult(!Disposed);
                    },
                    (e, childLogger) => Task.FromResult(!Disposed),
                    swallowException: true);
            });
            return pools.Count();
        }

        /// <summary>
        /// Log statistics about job to logger
        /// </summary>
        /// <param name="logger">Logger to write to</param>
        /// <param name="job">Job to check </param>
        /// <param name="batchClient">Batch client</param>
        /// <param name="taskQuery">Query to get tasks inside the job</param>
        /// <param name="taskCounts">Array to hold task information</param>
        public void LogJobStats(IDiagnosticsLogger logger, CloudJob job, BatchClient batchClient, ODATADetailLevel taskQuery, TaskCounts taskCounts)
        {
            logger.OperationScopeAsync(
                    $"{LogBaseName}_job_status",
                    async (childLogger) =>
                    {
                        List<double> taskTimes = new List<double>();
                        var displayName = job.DisplayName;

                        childLogger.FluentAddValue("ActiveTasks", taskCounts.Active)
                                .FluentAddValue("RunningTasks", taskCounts.Running)
                                .FluentAddValue("CompletedTasks", taskCounts.Completed)
                                .FluentAddValue("SucceededTasks", taskCounts.Succeeded)
                                .FluentAddValue("FailedTasks", taskCounts.Failed)
                                .FluentAddValue("DisplayName", job.DisplayName)
                                .FluentAddValue("JobId", job.Id);
                        if (taskCounts.Completed > 0)
                        {
                            childLogger.FluentAddValue("TotalSuccessRate", taskCounts.Succeeded / (double)taskCounts.Completed);
                        }

                        var successRate = await GetTaskTimes(job.Id, taskQuery, taskTimes, batchClient);
                        if (successRate.HasValue)
                        {
                            childLogger.FluentAddValue("CurrentSuccessRate", successRate);
                        }

                        if (displayName.Equals(ArchiveTaskDisplayName))
                        {
                            childLogger.FluentAddValue("TaskType", "Archive");
                        }
                        else if (displayName.Equals(PrepareTaskDisplayName))
                        {
                            childLogger.FluentAddValue("TaskType", "Prepare");
                        }

                        if (taskTimes.Count > 0)
                        {
                            childLogger.FluentAddValue("AverageExecutionTimeSec", taskTimes.Average())
                                  .FluentAddValue("MinExecutionTimeSec", taskTimes.Min())
                                  .FluentAddValue("MaxExecutionTimeSec", taskTimes.Max())
                                  .FluentAddValue("TaskCount", taskTimes.Count());
                        }

                        childLogger.LogInfo($"{LogBaseName}_job_status");
                    },
                    (e, childLogger) => Task.FromResult(!Disposed),
                    swallowException: true);
        }

        /// <summary>
        /// Gets info on failed tasks and writes to logger
        /// </summary>
        /// <param name="logger">Logger used to create child loggers.</param>
        /// <param name="job">Job to check for tasks.</param>
        /// <param name="batchClient">Batch client</param>
        /// <param name="failedTaskQuery"> Query to get recent failed tasks</param>
        public async Task LogFailedTasks(IDiagnosticsLogger logger, CloudJob job, BatchClient batchClient, ODATADetailLevel failedTaskQuery)
        {
            var tasks = batchClient.JobOperations.ListTasks(job.Id, failedTaskQuery);
            string taskType = string.Empty;
            if (job.DisplayName.Equals(ArchiveTaskDisplayName))
            {
                taskType = "Archive";
            }
            else if (job.DisplayName.Equals(PrepareTaskDisplayName))
            {
                taskType = "Prepare";
            }

            if (tasks != null)
            {
                await tasks.ForEachAsync(delegate(CloudTask task)
                {
                    if (task.ExecutionInformation != null)
                    {
                        if (task.ExecutionInformation.Result != TaskExecutionResult.Success)
                        {
                            logger.OperationScopeAsync(
                                $"{LogBaseName}_task_failure",
                                (childLogger) =>
                                {
                                    childLogger.FluentAddValue("ExitCode", task.ExecutionInformation.ExitCode)
                                    .FluentAddValue("EndTime", task.ExecutionInformation.EndTime)
                                    .FluentAddValue("StartTime", task.ExecutionInformation.StartTime)
                                    .FluentAddValue("RetryCount", task.ExecutionInformation.RetryCount)
                                    .FluentAddValue("ReqeueCount", task.ExecutionInformation.RequeueCount)
                                    .FluentAddValue("TaskType", taskType)
                                    .FluentAddValue("TaskId", task.Id)
                                    .FluentAddValue("ETag", task.ETag);
                                    if (task.ExecutionInformation.FailureInformation != null)
                                    {
                                        var details = task.ExecutionInformation.FailureInformation.Details;
                                        var detailsString = string.Join(System.Environment.NewLine, details.Select((detail) => $"{detail.Name} : {detail.Value}"));

                                        childLogger.FluentAddValue("FailureCategory", task.ExecutionInformation.FailureInformation.Category)
                                        .FluentAddValue("FailureCode", task.ExecutionInformation.FailureInformation.Code)
                                        .FluentAddValue("FailureMessage", task.ExecutionInformation.FailureInformation.Message)
                                        .FluentAddValue("FailureDetails", detailsString);
                                    }

                                    if (task.ComputeNodeInformation != null)
                                    {
                                        childLogger.FluentAddValue("ComputeNodeId", task.ComputeNodeInformation.ComputeNodeId)
                                        .FluentAddValue("PoolId", task.ComputeNodeInformation.PoolId)
                                        .FluentAddValue("AffinityId", task.ComputeNodeInformation.AffinityId)
                                        .FluentAddValue("TaskRootDirectory", task.ComputeNodeInformation.TaskRootDirectory)
                                        .FluentAddValue("ComputeNodeUrl", task.ComputeNodeInformation.ComputeNodeUrl)
                                        .FluentAddValue("TaskRootDirectoryUrl", task.ComputeNodeInformation.TaskRootDirectoryUrl);
                                    }

                                    childLogger.LogInfo($"{LogBaseName}_task_failure");
                                    return Task.FromResult(!Disposed);
                                },
                                (e, childLogger) => Task.FromResult(!Disposed),
                                swallowException: true);
                        }
                    }
                });
            }
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
                List<string> displayNames = new List<string>();
                int activeTasks = 0;
                int runningTasks = 0;
                int completedTasks = 0;
                int succeededTasks = 0;
                int failedTasks = 0;
                var finishedAfter = DateTime.UtcNow.AddMinutes(-30);
                var lastFiveMin = DateTime.UtcNow.AddMinutes(-5);
                var jobsOdataQuery = new ODATADetailLevel(
                    filterClause: $"executionInfo/poolId eq '{StorageProviderSettings.WorkerBatchPoolId}' and state eq 'active'",
                    selectClause: "id,displayName");
                var taskOdataQuery = new ODATADetailLevel(
                    filterClause: $"executionInfo/endTime ge DateTime'{finishedAfter:o}' and state eq 'Completed'");
                var failedTaskQuery = new ODATADetailLevel(
                    filterClause: $"executionInfo/endTime ge DateTime'{lastFiveMin:o}' and state eq 'Completed'");

                var jobs = batchClient.JobOperations.ListJobs(jobsOdataQuery);
                var count = jobs.Count();
                await jobs.ForEachAsync(async delegate(CloudJob job)
                {
                    var displayName = job.DisplayName;
                    TaskCounts taskCounts = await batchClient.JobOperations.GetJobTaskCountsAsync(job.Id);
                    LogJobStats(logger.NewChildLogger(), job, batchClient, taskOdataQuery, taskCounts);
                    _ = LogFailedTasks(logger, job, batchClient, failedTaskQuery);
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
                    else
                    {
                        displayNames.Add(displayName);
                    }
                });

                logger.FluentAddValue("ActiveTasks", activeTasks)
                    .FluentAddValue("RunningTasks", runningTasks)
                    .FluentAddValue("CompletedTasks", completedTasks)
                    .FluentAddValue("SucceededTasks", succeededTasks)
                    .FluentAddValue("FailedTasks", failedTasks)
                    .FluentAddValue("IrregularDisplayNames", string.Join(",", displayNames.ToArray()));

                if (completedTasks > 0)
                {
                    logger.FluentAddValue("SuccessRate", (succeededTasks * 100 / (double)completedTasks));
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
