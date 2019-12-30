// <copyright file="StorageFileShareProviderHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.File;
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
        private const string StorageSkuNameStandard = "Standard_LRS";
        private const string StorageSkuNamePremium = "Premium_LRS";
        private static readonly int StorageAccountNameMaxLength = 24;
        private static readonly int StorageAccountNameGenerateMaxAttempts = 3;
        private static readonly int StorageShareQuotaGb = 100;
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
            string azureSkuName,
            IDictionary<string, string> resourceTags,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(azureRegion, nameof(azureRegion));
            Requires.NotNullOrEmpty(azureResourceGroup, nameof(azureResourceGroup));
            Requires.NotNullOrEmpty(azureSubscriptionId, nameof(azureSubscriptionId));
            Requires.NotNullOrEmpty(azureSkuName, nameof(azureSkuName));
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
                bool isPremiumSku;
                switch (azureSkuName)
                {
                    case StorageSkuNamePremium:
                        isPremiumSku = true;
                        break;
                    case StorageSkuNameStandard:
                        isPremiumSku = false;
                        break;
                    default:
                        throw new ArgumentException($"Unable to handle creation of storage account with sku of {azureSkuName}");
                }

                await azure.CreateResourceGroupIfNotExistsAsync(azureResourceGroup, azureRegion);
                var storageAccountName = await GenerateStorageAccountName(azure, logger);

                resourceTags.Add(ResourceTagName.ResourceName, storageAccountName);

                // Premium_LRS for Files requires a different kind of FileStorage
                // See https://docs.microsoft.com/en-us/azure/storage/common/storage-account-overview#types-of-storage-accounts
                var storageCreateParams = new StorageAccountCreateParameters()
                {
                    Location = azureRegion,
                    EnableHttpsTrafficOnly = true,
                    Tags = resourceTags,
                    Kind = isPremiumSku ? Kind.FileStorage : Kind.StorageV2,
                    Sku = new SkuInner(isPremiumSku ? SkuName.PremiumLRS : SkuName.StandardLRS),
                };

                logger.FluentAddValue("AzureStorageAccountName", storageAccountName)
                    .FluentAddValue("AzureStorageAccountRegion", azureRegion)
                    .FluentAddValue("AzureStorageAccountKind", storageCreateParams.Kind.ToString())
                    .FluentAddValue("AzureStorageAccountSkuName", storageCreateParams.Sku.Name.ToString());

                await azure.StorageAccounts.Inner.CreateAsync(azureResourceGroup, storageAccountName, storageCreateParams);

                logger.LogInfo("file_share_storage_provider_helper_create_storage_account_complete");

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
                fileShare.Properties.Quota = StorageShareQuotaGb;
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
            int storageSizeInGb,
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

                var desiredBatchLocation = storageAccount.RegionName;

                using (var batchClient = await batchClientFactory.GetBatchClient(desiredBatchLocation, logger))
                {
                    var job = await GetOrCreateJob(batchClient, sourceCopyItems, storageSizeInGb);

                    var taskId = azureResourceInfo.Name;
                    var task = ConstructTask(job.Id, taskId, fileShare, sourceCopyItems, logger);

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
                using (var batchClient = await batchClientFactory.GetBatchClient(taskInfo.TaskLocation, logger))
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
        private string GetBatchJobIdFromBlobFilename(IEnumerable<StorageCopyItem> sourceCopyItems, int jobSuffix) => $"{(storageProviderSettings.WorkerBatchPoolId + JoinCopyItemFileNames(sourceCopyItems)).GetDeterministicHashCode()}_{jobSuffix}";

        private string GetJobWorkingDirectory(string jobId) => $"/datadrive/images/{jobId}/";

        /// <summary>
        /// Gets an Azure Batch job that can have tasks added to it.
        /// If a job doesn't already exist, it will create one.
        /// Includes retry logic for race conditions of multiple workers attempting to create the same job.
        /// </summary>
        /// <param name="batchClient">Batch client.</param>
        /// <param name="sourceCopyItems">Source copy items.</param>
        /// <returns>CloudJob.</returns>
        private async Task<CloudJob> GetOrCreateJob(BatchClient batchClient, IEnumerable<StorageCopyItem> sourceCopyItems, int storageSizeInGb)
        {
            var jobSuffix = 0;
            var attempts = 0;
            for (; attempts < 3; attempts++)
            {
                var jobId = GetBatchJobIdFromBlobFilename(sourceCopyItems, jobSuffix);
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
                    job = ConstructJob(batchClient, jobId, sourceCopyItems, storageSizeInGb);
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
        /// <param name="sourceCopyItems">Source storage copy items.</param>
        /// <returns>CloudJob.</returns>
        private CloudJob ConstructJob(BatchClient batchClient, string jobId, IEnumerable<StorageCopyItem> sourceCopyItems, int storageSizeInGb)
        {
            var jobPrepareCommandLines = new List<string>();
            var jobReleaseCommandLines = new List<string>();
            var jobMetadata = new List<MetadataItem>();
            var jobWorkingDir = GetJobWorkingDirectory(jobId);

            foreach (var copyItem in sourceCopyItems)
            {
                jobPrepareCommandLines.Add($"$AZ_BATCH_NODE_SHARED_DIR/azcopy cp '{copyItem.SrcBlobUrl}' {jobWorkingDir}{copyItem.SrcBlobFileName}");
                if (copyItem.StorageType == StorageType.Linux)
                {
                    jobPrepareCommandLines.Add($"resize2fs -fp {jobWorkingDir}{copyItem.SrcBlobFileName} {storageSizeInGb}G");
                }

                jobMetadata.Add(new MetadataItem($"{batchJobMetadataKey}-{copyItem.StorageType}", copyItem.SrcBlobFileName));
            }

            jobReleaseCommandLines.Add($"rm -rf {jobWorkingDir}");

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

        private CloudTask ConstructTask(
            string jobId,
            string taskId,
            CloudFileShare fileShare,
            IEnumerable<StorageCopyItem> sourceCopyItems,
            IDiagnosticsLogger logger)
        {
            var jobWorkingDir = GetJobWorkingDirectory(jobId);

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
                taskCopyCommands.Add($"$AZ_BATCH_NODE_SHARED_DIR/azcopy cp {jobWorkingDir}{copyItem.SrcBlobFileName} '{destFileUriWithSas}'");

                logger.FluentAddValue($"DestinationStorageFilePath-{copyItem.StorageType}", destFile.Uri.ToString());
            }

            // Define the task
            var taskCommandLine = $"/bin/bash -cxe \"printenv && {string.Join(" && ", taskCopyCommands)}\"";

            var task = new CloudTask(taskId, taskCommandLine)
            {
                Constraints = new TaskConstraints(maxTaskRetryCount: 3, maxWallClockTime: TimeSpan.FromMinutes(10), retentionTime: TimeSpan.FromDays(1)),
            };
            return task;
        }

        private async Task<CloudJob> GetExistingJob(BatchClient batchClient, string jobId)
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
    }
}
