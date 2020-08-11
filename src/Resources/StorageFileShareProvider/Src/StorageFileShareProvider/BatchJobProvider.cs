// <copyright file="BatchJobProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Settings;
using Newtonsoft.Json;
using JobState = Microsoft.Azure.Batch.Common.JobState;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
{
    /// <summary>
    /// Batch Job Provider base class.
    /// </summary>
    /// <typeparam name="T">Type of input.</typeparam>
    public abstract class BatchJobProvider<T> : IBatchJobProvider<T>
        where T : BatchTaskInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BatchJobProvider{T}"/> class.
        /// </summary>
        /// <param name="storageFileShareProviderHelper">Target Storage File Share Provider Helper.</param>
        /// <param name="batchClientFactory">Batch client factory.</param>
        /// <param name="azureClientFactory">Azure client factory.</param>
        /// <param name="storageProviderSettings">The storage provider settings.</param>
        public BatchJobProvider(
            IStorageFileShareProviderHelper storageFileShareProviderHelper,
            IBatchClientFactory batchClientFactory,
            IAzureClientFactory azureClientFactory,
            StorageProviderSettings storageProviderSettings)
        {
            StorageFileShareProviderHelper = Requires.NotNull(storageFileShareProviderHelper, nameof(storageFileShareProviderHelper));
            BatchClientFactory = Requires.NotNull(batchClientFactory, nameof(batchClientFactory));
            AzureClientFactory = Requires.NotNull(azureClientFactory, nameof(azureClientFactory));
            StorageProviderSettings = Requires.NotNull(storageProviderSettings, nameof(storageProviderSettings));
        }

        /// <summary>
        /// Gets the batch client factory.
        /// </summary>
        protected IStorageFileShareProviderHelper StorageFileShareProviderHelper { get; }

        /// <summary>
        /// Gets the batch client factory.
        /// </summary>
        protected IBatchClientFactory BatchClientFactory { get; }

        /// <summary>
        /// Gets the stroage provider settings.
        /// </summary>
        protected StorageProviderSettings StorageProviderSettings { get; }

        /// <summary>
        /// Gets the azure client factory.
        /// </summary>
        protected IAzureClientFactory AzureClientFactory { get; }

        /// <summary>
        /// Gets the log name that should be used.
        /// </summary>
        protected abstract string LogBaseName { get; }

        /// <inheritdoc/>
        public Task<BatchTaskStatus> CheckBatchTaskStatusAsync(
            AzureResourceInfo azureResourceInfo,
            BatchTaskInfo taskInfo,
            TimeSpan maxWaitTime,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_check_batch_task_status",
                async (childLogger) =>
                {
                    childLogger.FluentAddValue("AzureStorageAccountName", azureResourceInfo.Name);

                    using (var batchClient = await BatchClientFactory.GetBatchClient(taskInfo.TaskLocation, childLogger))
                    {
                        var task = await batchClient.JobOperations.GetTaskAsync(taskInfo.JobId, taskInfo.TaskId);

                        childLogger.FluentAddValue("TaskId", task.Id)
                            .FluentAddValue("TaskJobId", taskInfo.JobId)
                            .FluentAddValue("TaskUrl", task.Url)
                            .FluentAddValue("TaskState", task.State)
                            .FluentAddValue("TaskCreatedAt", task.CreationTime)
                            .FluentAddValue("TaskStateTransitionTime", task.StateTransitionTime)
                            .FluentAddValue("TaskStartedAt", task.ExecutionInformation.StartTime)
                            .FluentAddValue("TaskCompletedAt", task.ExecutionInformation.EndTime)
                            .FluentAddValue("TaskRequeueCount", task.ExecutionInformation.RequeueCount)
                            .FluentAddValue("TaskRetryCount", task.ExecutionInformation.RetryCount);

                        BatchTaskStatus batchTaskStatus;

                        // To know if a task failed, state will be 'completed' but not always will executionInformation.Result
                        // be set to Failure so we check if FailureInformation is set as well.
                        if (task.State == TaskState.Completed
                            && (task.ExecutionInformation.Result == TaskExecutionResult.Failure
                                || task.ExecutionInformation.FailureInformation != null))
                        {
                            var failureInfo = task.ExecutionInformation.FailureInformation;

                            childLogger.FluentAddValue("TaskFailureCategory", failureInfo.Category)
                                .FluentAddValue("TaskFailureCode", failureInfo.Code)
                                .FluentAddValue("TaskFailureDetails", JsonConvert.SerializeObject(failureInfo.Details))
                                .FluentAddValue("TaskFailureMessage", failureInfo.Message);

                            batchTaskStatus = BatchTaskStatus.Failed;
                        }
                        else if (task.State == TaskState.Completed && task.ExecutionInformation.Result == TaskExecutionResult.Success)
                        {
                            batchTaskStatus = BatchTaskStatus.Succeeded;
                        }
                        else if (task.State == TaskState.Running)
                        {
                            batchTaskStatus = BatchTaskStatus.Running;
                        }
                        else
                        {
                            batchTaskStatus = BatchTaskStatus.Pending;
                            if (task.State == TaskState.Active && maxWaitTime != default)
                            {
                                var pendingDeadline = task.CreationTime + maxWaitTime;
                                var pendingDeadlineExceeded = pendingDeadline < DateTime.UtcNow;
                                childLogger.FluentAddValue("TaskPendingTimeout", maxWaitTime)
                                    .FluentAddValue("TaskPendingDeadlineTime", pendingDeadline)
                                    .FluentAddValue("TaskPendingDeadlineExceeded", pendingDeadlineExceeded);
                                if (pendingDeadlineExceeded)
                                {
                                    await task.TerminateAsync();
                                    childLogger.FluentAddValue("TaskFailureCategory", ErrorCategory.UserError)
                                        .FluentAddValue("TaskFailureCode", "TaskPendingDeadlineExceeded")
                                        .FluentAddValue("TaskFailureMessage", "Task took too long to start running");
                                    batchTaskStatus = BatchTaskStatus.Failed;
                                }
                            }
                        }

                        childLogger.FluentAddValue("TaskBatchStatus", batchTaskStatus);

                        return batchTaskStatus;
                    }
                });
        }

        /// <inheritdoc/>
        public Task<BatchTaskInfo> StartBatchTaskAsync(
            AzureResourceInfo azureResourceInfo,
            T taskInput,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_start_batch_task",
                async (childLogger) =>
                {
                    logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

                    var azureSubscriptionId = azureResourceInfo.SubscriptionId;
                    var azure = await AzureClientFactory.GetAzureClientAsync(azureSubscriptionId);

                    var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(azureResourceInfo.ResourceGroup, azureResourceInfo.Name);
                    var storageAccountName = storageAccount.Name;
                    var storageAccountKey = await StorageFileShareProviderHelper.GetStorageAccountKey(storageAccount);
                    var storageCreds = new StorageCredentials(storageAccountName, storageAccountKey);

                    Uri.TryCreate(storageAccount.EndPoints.Primary.Blob, UriKind.Absolute, out var blobEndpoint);
                    Uri.TryCreate(storageAccount.EndPoints.Primary.Queue, UriKind.Absolute, out var queueEndpoint);
                    Uri.TryCreate(storageAccount.EndPoints.Primary.Table, UriKind.Absolute, out var tableEndpoint);
                    Uri.TryCreate(storageAccount.EndPoints.Primary.File, UriKind.Absolute, out var fileEndpoint);

                    var cloudStorageAccount = new CloudStorageAccount(storageCreds, blobEndpoint, queueEndpoint, tableEndpoint, fileEndpoint);
                    var fileClient = cloudStorageAccount.CreateCloudFileClient();
                    var fileShare = fileClient.GetShareReference(StorageFileShareProviderHelper.GetStorageMountableShareName());

                    var desiredBatchLocation = storageAccount.RegionName;

                    using (var batchClient = await BatchClientFactory.GetBatchClient(desiredBatchLocation, logger))
                    {
                        var job = await GetOrCreateJob(batchClient, taskInput);

                        var taskId = GetBatchTaskIdFromInput(taskInput, azureResourceInfo);
                        var task = ConstructTask(job.Id, taskId, fileShare, taskInput, logger);

                        await job.AddTaskAsync(task);

                        logger.FluentAddValue("TaskId", taskId)
                            .FluentAddValue("TaskJobId", job.Id);

                        return new BatchTaskInfo(job.Id, task.Id, desiredBatchLocation);
                    }
                });
        }

        /// <summary>
        /// Gets an Azure Batch job that can have tasks added to it.
        /// If a job doesn't already exist, it will create one.
        /// Includes retry logic for race conditions of multiple workers attempting to create the same job.
        /// </summary>
        /// <param name="batchClient">Batch client.</param>
        /// <param name="taskInput">Target task input.</param>
        /// <returns>CloudJob.</returns>
        protected virtual async Task<CloudJob> GetOrCreateJob(BatchClient batchClient, T taskInput)
        {
            var jobSuffix = 0;
            var attempts = 0;
            for (; attempts < 3; attempts++)
            {
                var jobId = GetBatchJobIdFromInput(taskInput, jobSuffix);
                var job = await GetExistingJob(batchClient, jobId);
                if (job != null)
                {
                    // If job is in an active/available state, return it.
                    if (job.State != JobState.Active)
                    {
                        // Increment job suffix so we can try a new unique job id
                        jobSuffix++;
                        continue;
                    }

                    return job;
                }
                else
                {
                    job = ConstructJob(batchClient, jobId, taskInput);
                    try
                    {
                        await job.CommitAsync();
                        await job.RefreshAsync();
                        return job;
                    }
                    catch (BatchException ex)
                    {
                        // The job didn't exist so we tried to create it but someone beat us to it!
                        // Continue the loop to get the job that someone else created.
                        if (ex.Message != null && ex.Message.Contains("Conflict"))
                        {
                            continue;
                        }

                        throw ex;
                    }
                }
            }

            throw new StorageCreateException($"Unable to get an Azure Batch job after {attempts} attempt(s).");
        }

        /// <summary>
        /// Constructs a new Azure Batch Cloud Job and returns this Job.
        /// This method *does not* actually submit this new job to the Azure Batch service.
        /// It's the caller's responsibility to call this after getting the CloudJob object back.
        /// </summary>
        /// <param name="batchClient">Batch client.</param>
        /// <param name="jobId">Id of the job to construct.</param>
        /// <param name="taskInput">Targget task input.</param>
        /// <returns>CloudJob.</returns>
        protected abstract CloudJob ConstructJob(
            BatchClient batchClient,
            string jobId,
            T taskInput);

        /// <summary>
        /// Constructs a new Azure Cloud Task and returns the task. This is the
        /// thing that will run every time we want to execute a given job definition.
        /// </summary>
        /// <param name="jobId">Target job id.</param>
        /// <param name="taskId">Target task id.</param>
        /// <param name="fileShare">Target fileshare.</param>
        /// <param name="taskInput">Targget task input.</param>
        /// <param name="logger">Targget logger.</param>
        /// <returns>CloudTask.</returns>
        protected abstract CloudTask ConstructTask(
            string jobId,
            string taskId,
            CloudFileShare fileShare,
            T taskInput,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Job ids can only contain any combination of alphanumeric characters along with dash (-) and underscore (_). The name must be from 1 through 64 characters long
        /// Source blob file names are used to find a Job so should be versioned (i.e. immutable files with no in-place updates).
        /// </summary>
        /// <param name="taskInput">Target task input.</param>
        /// <param name="jobSuffix">Target job suffix.</param>
        /// <returns>A job id.</returns>
        protected abstract string GetBatchJobIdFromInput(T taskInput, int jobSuffix);

        /// <summary>
        /// Construct task id based on avaialble input.
        /// </summary>
        /// <param name="taskInput">Target task input.</param>
        /// <param name="azureResourceInfo">Target azure resource info.</param>
        /// <returns>Generated task id.</returns>
        protected virtual string GetBatchTaskIdFromInput(T taskInput, AzureResourceInfo azureResourceInfo)
        {
            return azureResourceInfo.Name;
        }

        /// <summary>
        /// Gets an existing job if available from azure batch.
        /// </summary>
        /// <param name="batchClient">Target batch client.</param>
        /// <param name="jobId">Target job id.</param>
        /// <returns>Batch job if it is found.</returns>
        protected async Task<CloudJob> GetExistingJob(BatchClient batchClient, string jobId)
        {
            try
            {
                var job = await batchClient.JobOperations.GetJobAsync(jobId);
                return job;
            }
            catch (BatchException ex)
            {
                if (ex.Message != null && ex.Message.Contains("NotFound"))
                {
                    // Didn't find the job
                    return null;
                }

                throw;
            }
        }

        /// <summary>
        /// Gets the working directory for this Job.
        /// </summary>
        /// <param name="jobId">Target Job Id.</param>
        /// <returns>Working directory.</returns>
        protected string GetJobWorkingDirectory(string jobId) => $"/datadrive/images/{jobId}/";
    }
}
