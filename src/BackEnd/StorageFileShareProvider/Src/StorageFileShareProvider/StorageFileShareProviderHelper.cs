// <copyright file="StorageFileShareProviderHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Settings;
using Newtonsoft.Json;
using JobState = Microsoft.Azure.Batch.Common.JobState;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
{
    /// <summary>
    /// Implements <see cref="IStorageFileShareProviderHelper"/> using Azure SDKs and APIs.
    /// </summary>
    public class StorageFileShareProviderHelper : IStorageFileShareProviderHelper
    {
        private static readonly int StorageAccountNameMaxLength = 24;
        private static readonly int StorageAccountNameGenerateMaxAttempts = 3;
        private static readonly string StorageMountableShareName = "cloudenvdata";
        private static readonly string StorageAccountNamePrefix = "vsoce";
        private static readonly string StorageLinuxMountableFilename = "dockerlib";
        private static readonly string StorageWindowsMountableFilename = "windowsdisk.vhdx";
        private readonly string batchJobMetadataKey = "SourceBlobFilename";
        private readonly ISystemCatalog systemCatalog;
        private readonly IBatchClientFactory batchClientFactory;
        private readonly StorageProviderSettings storageProviderSettings;
        private readonly IAzureClientFactory azureClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageFileShareProviderHelper"/> class.
        /// </summary>
        /// <param name="systemCatalog">System catalog.</param>
        /// <param name="batchClientFactory">Batch client factory.</param>
        /// <param name="storageProviderSettings">The storage provider settings.</param>
        public StorageFileShareProviderHelper(
            ISystemCatalog systemCatalog,
            IBatchClientFactory batchClientFactory,
            StorageProviderSettings storageProviderSettings)
        {
            this.systemCatalog = Requires.NotNull(systemCatalog, nameof(systemCatalog));
            this.batchClientFactory = Requires.NotNull(batchClientFactory, nameof(batchClientFactory));
            this.storageProviderSettings = Requires.NotNull(storageProviderSettings, nameof(storageProviderSettings));
            azureClientFactory = new AzureClientFactory(this.systemCatalog);
        }

        /// <inheritdoc/>
        public async Task<AzureResourceInfo> CreateStorageAccountAsync(
            string azureSubscriptionId,
            string azureRegion,
            string azureResourceGroup,
            IDictionary<string, string> resourceTags,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(azureRegion, nameof(azureRegion));
            Requires.NotNullOrEmpty(azureResourceGroup, nameof(azureResourceGroup));
            Requires.NotNullOrEmpty(azureSubscriptionId, nameof(azureSubscriptionId));
            Requires.NotNull(resourceTags, nameof(resourceTags));

            logger = logger.WithValues(new LogValueSet
            {
                { "AzureRegion", azureRegion },
                { "AzureResourceGroup", azureResourceGroup },
                { "AzureSubscription", azureSubscriptionId },
            });
            var azure = await azureClientFactory.GetAzureClientAsync(new Guid(azureSubscriptionId));

            try
            {
                await azure.CreateResourceGroupIfNotExistsAsync(azureResourceGroup, azureRegion);
                var storageAccountName = await GenerateStorageAccountName(azure, logger);

                resourceTags.Add(ResourceTagName.ResourceName, storageAccountName);

                var storageAccount = await azure.StorageAccounts.Define(storageAccountName)
                    .WithRegion(azureRegion)
                    .WithExistingResourceGroup(azureResourceGroup)
                    .WithGeneralPurposeAccountKindV2()
                    .WithOnlyHttpsTraffic()
                    .WithSku(StorageAccountSkuType.Standard_LRS)
                    .WithTags(resourceTags)
                    .CreateAsync();

                logger.FluentAddValue("AzureStorageAccountName", storageAccountName)
                    .LogInfo("file_share_storage_provider_helper_create_storage_account_complete");

                return new AzureResourceInfo(Guid.Parse(azureSubscriptionId), azureResourceGroup, storageAccountName);
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_create_storage_account_error", ex);

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task CreateFileShareAsync(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            var azureSubscriptionId = azureResourceInfo.SubscriptionId;
            var azure = await azureClientFactory.GetAzureClientAsync(azureSubscriptionId);

            try
            {
                var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(azureResourceInfo.ResourceGroup, azureResourceInfo.Name);
                var storageAccountName = storageAccount.Name;
                var storageAccountKey = await GetStorageAccountKey(storageAccount);
                var storageCreds = new StorageCredentials(storageAccountName, storageAccountKey);
                var cloudStorageAccount = new CloudStorageAccount(storageCreds, useHttps: true);
                var fileClient = cloudStorageAccount.CreateCloudFileClient();
                var fileShare = fileClient.GetShareReference(StorageMountableShareName);
                await fileShare.CreateIfNotExistsAsync();
                logger.LogInfo("file_share_storage_provider_helper_create_file_share_complete");
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_create_file_share_error", ex);
                throw ex;
            }
        }

        /// <inheritdoc/>
        public async Task<PrepareFileShareTaskInfo> StartPrepareFileShareAsync(
            AzureResourceInfo azureResourceInfo,
            IEnumerable<StorageCopyItem> sourceCopyItems,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            Requires.NotNullOrEmpty(sourceCopyItems, nameof(sourceCopyItems));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            var azureSubscriptionId = azureResourceInfo.SubscriptionId;
            var azure = await azureClientFactory.GetAzureClientAsync(azureSubscriptionId);

            try
            {
                var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(azureResourceInfo.ResourceGroup, azureResourceInfo.Name);
                var storageAccountName = storageAccount.Name;
                var storageAccountKey = await GetStorageAccountKey(storageAccount);
                var storageCreds = new StorageCredentials(storageAccountName, storageAccountKey);
                var cloudStorageAccount = new CloudStorageAccount(storageCreds, useHttps: true);
                var fileClient = cloudStorageAccount.CreateCloudFileClient();
                var fileShare = fileClient.GetShareReference(StorageMountableShareName);

                // The SaS tokens applies to the entire file share where all items are copied to.
                var destFileSas = fileShare.GetSharedAccessSignature(new SharedAccessFilePolicy()
                {
                    Permissions = SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Write,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(4),
                });

                var taskCopyCommands = new List<string>();
                foreach (var copyItem in sourceCopyItems)
                {
                    var destFile = fileShare.GetRootDirectoryReference().GetFileReference(GetStorageMountableFileName(copyItem.StorageType));
                    var destFileUriWithSas = destFile.Uri.AbsoluteUri + destFileSas;
                    taskCopyCommands.Add($"$AZ_BATCH_NODE_SHARED_DIR/azcopy cp $AZ_BATCH_NODE_SHARED_DIR/'{copyItem.SrcBlobFileName}' '{destFileUriWithSas}'");

                    logger.FluentAddValue($"DestinationStorageFilePath-{copyItem.StorageType}", destFile.Uri.ToString());
                }

                // Define the task
                var taskCommandLine = $"/bin/bash -cxe \"printenv && {string.Join(" && ", taskCopyCommands)}\"";
                var taskId = azureResourceInfo.Name;
                var task = new CloudTask(taskId, taskCommandLine)
                {
                    Constraints = new TaskConstraints(maxTaskRetryCount: 3, maxWallClockTime: TimeSpan.FromMinutes(10), retentionTime: TimeSpan.FromDays(1)),
                };

                var desiredBatchLocation = storageAccount.RegionName;

                using (BatchClient batchClient = await batchClientFactory.GetBatchClient(desiredBatchLocation, logger))
                {
                    var job = await GetOrCreateJob(batchClient, sourceCopyItems);

                    await job.AddTaskAsync(task);

                    logger.FluentAddValue("TaskId", taskId)
                        .FluentAddValue("TaskJobId", job.Id)
                        .LogInfo("file_share_storage_provider_helper_start_prepare_file_share_complete");

                    return new PrepareFileShareTaskInfo(job.Id, task.Id, desiredBatchLocation);
                }
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_start_prepare_file_share_error", ex);

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<PrepareFileShareStatus> CheckPrepareFileShareAsync(
            AzureResourceInfo azureResourceInfo,
            PrepareFileShareTaskInfo taskInfo,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            PrepareFileShareStatus prepareStatus;

            try
            {
                using (BatchClient batchClient = await batchClientFactory.GetBatchClient(taskInfo.TaskLocation, logger))
                {
                    var task = await batchClient.JobOperations.GetTaskAsync(taskInfo.JobId, taskInfo.TaskId);

                    logger.FluentAddValue("TaskId", task.Id)
                        .FluentAddValue("TaskJobId", taskInfo.JobId)
                        .FluentAddValue("TaskUrl", task.Url)
                        .FluentAddValue("TaskState", task.State)
                        .FluentAddValue("TaskStateTransitionTime", task.StateTransitionTime)
                        .FluentAddValue("TaskStartedAt", task.ExecutionInformation.StartTime)
                        .FluentAddValue("TaskCompletedAt", task.ExecutionInformation.EndTime)
                        .FluentAddValue("TaskRequeueCount", task.ExecutionInformation.RequeueCount)
                        .FluentAddValue("TaskRetryCount", task.ExecutionInformation.RetryCount);

                    // To know if a task failed, state will be 'completed' but not always will executionInformation.Result
                    // be set to Failure so we check if FailureInformation is set as well.
                    if (task.State == TaskState.Completed
                        && (task.ExecutionInformation.Result == TaskExecutionResult.Failure
                            || task.ExecutionInformation.FailureInformation != null))
                    {
                        var failureInfo = task.ExecutionInformation.FailureInformation;
                        logger.FluentAddValue("TaskFailureCategory", failureInfo.Category)
                            .FluentAddValue("TaskFailureCode", failureInfo.Code)
                            .FluentAddValue("TaskFailureDetails", JsonConvert.SerializeObject(failureInfo.Details))
                            .FluentAddValue("TaskFailureMessage", failureInfo.Message)
                            .LogError("file_share_storage_provider_helper_check_prepare_file_share_failed");
                        return PrepareFileShareStatus.Failed;
                    }
                    else if (task.State == TaskState.Completed && task.ExecutionInformation.Result == TaskExecutionResult.Success)
                    {
                        prepareStatus = PrepareFileShareStatus.Succeeded;
                    }
                    else if (task.State == TaskState.Running)
                    {
                        prepareStatus = PrepareFileShareStatus.Running;
                    }
                    else
                    {
                        prepareStatus = PrepareFileShareStatus.Pending;
                    }

                    logger.LogInfo("file_share_storage_provider_helper_check_prepare_file_share_complete");
                    return prepareStatus;
                }
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_check_prepare_file_share_error", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ShareConnectionInfo> GetConnectionInfoAsync(
            AzureResourceInfo azureResourceInfo,
            StorageType storageType,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            var azureSubscriptionId = azureResourceInfo.SubscriptionId;
            var azure = await azureClientFactory.GetAzureClientAsync(azureSubscriptionId);

            try
            {
                var storageAccount = await azure.StorageAccounts.GetByResourceGroupAsync(azureResourceInfo.ResourceGroup, azureResourceInfo.Name);
                var storageAccountName = storageAccount.Name;
                var storageAccountKey = await GetStorageAccountKey(storageAccount);
                var shareConnectionInfo = new ShareConnectionInfo(
                    storageAccountName,
                    storageAccountKey,
                    StorageMountableShareName,
                    GetStorageMountableFileName(storageType));
                logger.LogInfo("file_share_storage_provider_helper_connection_info_complete");
                return shareConnectionInfo;
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_connection_info_error", ex);
                throw ex;
            }
        }

        /// <inheritdoc/>
        public async Task DeleteStorageAccountAsync(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            logger = logger.WithValue("AzureStorageAccountName", azureResourceInfo.Name);

            var azureSubscriptionId = azureResourceInfo.SubscriptionId;
            var azure = await azureClientFactory.GetAzureClientAsync(azureSubscriptionId);

            try
            {
                await azure.StorageAccounts.DeleteByResourceGroupAsync(azureResourceInfo.ResourceGroup, azureResourceInfo.Name);
                logger.LogInfo("file_share_storage_provider_helper_delete_storage_account_complete");
            }
            catch (Exception ex)
            {
                logger.LogException("file_share_storage_provider_helper_delete_storage_account_error", ex);
                throw;
            }
        }

        private async Task<string> GenerateStorageAccountName(IAzure azure, IDiagnosticsLogger logger)
        {
            Requires.NotNull(azure, nameof(azure));

            var charsAvailable = StorageAccountNameMaxLength - StorageAccountNamePrefix.Length;

            for (var attempts = 1; attempts <= StorageAccountNameGenerateMaxAttempts; attempts++)
            {
                var accountGuid = Guid.NewGuid().ToString("N").Substring(0, charsAvailable);
                var storageAccountName = string.Concat(StorageAccountNamePrefix, accountGuid);

                var checkNameAvailabilityResult = await azure.StorageAccounts
                    .CheckNameAvailabilityAsync(storageAccountName);
                if (checkNameAvailabilityResult.IsAvailable == true)
                {
                    logger
                        .FluentAddValue("StorageAccountName", storageAccountName)
                        .FluentAddValue("AttemptsTaken", attempts.ToString())
                        .LogInfo("file_share_storage_provider_helper_generate_account_name_complete");
                    return storageAccountName;
                }
            }

            logger.LogError("file_share_storage_provider_helper_generate_account_name_error");
            throw new StorageCreateException("Unable to generate storage account name");
        }

        private async Task<string> GetStorageAccountKey(IStorageAccount storageAccount)
        {
            var keys = await storageAccount.GetKeysAsync();
            var key1 = keys[0].Value;
            return key1;
        }

        private string GetStorageMountableFileName(StorageType storageType) => storageType == StorageType.Linux ? StorageLinuxMountableFilename : StorageWindowsMountableFilename;

        private string JoinCopyItemFileNames(IEnumerable<StorageCopyItem> copyItems) => string.Join(",", copyItems.Select(s => $"{s.StorageType.ToString()}={s.SrcBlobFileName}"));

        /// <summary>
        /// Job ids can only contain any combination of alphanumeric characters along with dash (-) and underscore (_). The name must be from 1 through 64 characters long
        /// Source blob file names are used to find a Job so should be versioned (i.e. immutable files with no in-place updates).
        /// </summary>
        /// <param name="sourceCopyItems">Source copy items.</param>
        /// <returns>A job id.</returns>
        private string GetBatchJobIdFromBlobFilename(IEnumerable<StorageCopyItem> sourceCopyItems) => (storageProviderSettings.WorkerBatchPoolId + JoinCopyItemFileNames(sourceCopyItems)).GetDeterministicHashCode();

        /// <summary>
        /// Gets an Azure Batch job that can have tasks added to it.
        /// If a job doesn't already exist, it will create one.
        /// Includes retry logic for race conditions of multiple workers attempting to create the same job.
        /// </summary>
        /// <param name="batchClient">Batch client.</param>
        /// <param name="sourceCopyItems">Source copy items.</param>
        /// <returns>CloudJob.</returns>
        private async Task<CloudJob> GetOrCreateJob(BatchClient batchClient, IEnumerable<StorageCopyItem> sourceCopyItems)
        {
            for (var attempts = 0; attempts < 3; attempts++)
            {
                var job = await GetExistingJob(batchClient, sourceCopyItems);
                if (job != null)
                {
                    return job;
                }
                else
                {
                    job = ConstructJob(batchClient, sourceCopyItems);
                    try
                    {
                        await job.CommitAsync();
                        await job.RefreshAsync();
                        return job;
                    }
                    catch (BatchException ex)
                    {
                        if (ex.Message != null && ex.Message.Contains("Conflict"))
                        {
                            continue;
                        }

                        throw ex;
                    }
                }
            }

            throw new StorageCreateException("Unable to get an Azure Batch job.");
        }

        /// <summary>
        /// Constructs a new Azure Batch Cloud Job and returns this Job.
        /// This method *does not* actually submit this new job to the Azure Batch service.
        /// It's the caller's responsibility to call this after getting the CloudJob object back.
        /// </summary>
        /// <param name="batchClient">Batch client.</param>
        /// <param name="sourceCopyItems">Source storage copy items.</param>
        /// <returns>CloudJob.</returns>
        private CloudJob ConstructJob(BatchClient batchClient, IEnumerable<StorageCopyItem> sourceCopyItems)
        {
            var jobPrepareCommandLines = new List<string>();
            var jobReleaseCommandLines = new List<string>();
            var jobMetadata = new List<MetadataItem>();

            foreach (var copyItem in sourceCopyItems)
            {
                jobPrepareCommandLines.Add($"$AZ_BATCH_NODE_SHARED_DIR/azcopy cp '{copyItem.SrcBlobUrl}' $AZ_BATCH_NODE_SHARED_DIR/'{copyItem.SrcBlobFileName}'");
                jobReleaseCommandLines.Add($"rm $AZ_BATCH_NODE_SHARED_DIR/'{copyItem.SrcBlobFileName}'");
                jobMetadata.Add(new MetadataItem($"{batchJobMetadataKey}-{copyItem.StorageType}", copyItem.SrcBlobFileName));
            }

            var jobId = GetBatchJobIdFromBlobFilename(sourceCopyItems);
            var poolInformation = new PoolInformation { PoolId = storageProviderSettings.WorkerBatchPoolId, };
            var job = batchClient.JobOperations.CreateJob(jobId, poolInformation);

            job.DisplayName = JoinCopyItemFileNames(sourceCopyItems);

            // Job preparation, release Task info - https://docs.microsoft.com/en-us/azure/batch/batch-job-prep-release#what-are-job-preparation-and-release-tasks
            var jobPrepareCommandLine = $"/bin/bash -cxe \"printenv && {string.Join(" && ", jobPrepareCommandLines)}\"";
            var jobReleaseCommandLine = $"/bin/bash -cxe \"printenv && {string.Join(" && ", jobReleaseCommandLines)}\"";
            job.JobPreparationTask = new JobPreparationTask
            {
                CommandLine = jobPrepareCommandLine,
                Constraints = new TaskConstraints
                {
                    RetentionTime = TimeSpan.FromDays(1),
                    MaxWallClockTime = TimeSpan.FromMinutes(10),
                    MaxTaskRetryCount = 3,
                },
                RerunOnComputeNodeRebootAfterSuccess = true,
                WaitForSuccess = true,
            };
            job.JobReleaseTask = new JobReleaseTask
            {
                CommandLine = jobReleaseCommandLine,
                RetentionTime = TimeSpan.FromDays(1),
                MaxWallClockTime = TimeSpan.FromMinutes(10),
            };
            job.Metadata = jobMetadata;
            job.Constraints = new JobConstraints(maxWallClockTime: TimeSpan.FromDays(90));

            return job;
        }

        private async Task<CloudJob> GetExistingJob(BatchClient batchClient, IEnumerable<StorageCopyItem> sourceCopyItems)
        {
            try
            {
                var job = await batchClient.JobOperations.GetJobAsync(GetBatchJobIdFromBlobFilename(sourceCopyItems));
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
    }
}
