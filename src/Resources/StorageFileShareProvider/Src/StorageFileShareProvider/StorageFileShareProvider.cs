// <copyright file="StorageFileShareProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
{
    /// <summary>
    /// Implementation of <see cref="IStorageProvider"/>.
    /// </summary>
    public class StorageFileShareProvider : IStorageProvider
    {
        private const int StorageCreationRetryAfterSeconds = 60;
        private readonly IStorageFileShareProviderHelper providerHelper;
        private readonly IBatchPrepareFileShareJobProvider batchPrepareFileShareJobProvider;
        private readonly IBatchArchiveFileShareJobProvider batchArchiveFileShareJobProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageFileShareProvider"/> class.
        /// This class acts as the provider interface for File Share Storage.
        /// </summary>
        /// <param name="providerHelper">An implementation of the <see cref="IStorageFileShareProviderHelper"/> interface.</param>
        /// <param name="batchPrepareFileShareJobProvider">An implementation of the <see cref="IBatchPrepareFileShareJobProvider"/> interface.</param>
        /// <param name="batchSuspendFileShareJobProvider">An implementation of the <see cref="IBatchArchiveFileShareJobProvider"/> interface.</param>
        public StorageFileShareProvider(
            IStorageFileShareProviderHelper providerHelper,
            IBatchPrepareFileShareJobProvider batchPrepareFileShareJobProvider,
            IBatchArchiveFileShareJobProvider batchSuspendFileShareJobProvider)
        {
            this.providerHelper = Requires.NotNull(providerHelper, nameof(providerHelper));
            this.batchPrepareFileShareJobProvider = Requires.NotNull(batchPrepareFileShareJobProvider, nameof(batchPrepareFileShareJobProvider));
            this.batchArchiveFileShareJobProvider = Requires.NotNull(batchSuspendFileShareJobProvider, nameof(batchSuspendFileShareJobProvider));
        }

        /// <inheritdoc/>
        public Task<FileShareProviderCreateResult> CreateAsync(
            FileShareProviderCreateInput input,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                "file_share_storage_provider_create_step",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(nameof(input.AzureSubscription), input.AzureSubscription)
                        .FluentAddBaseValue(nameof(input.AzureLocation), input.AzureLocation)
                        .FluentAddBaseValue(nameof(input.AzureResourceGroup), input.AzureResourceGroup)
                        .FluentAddBaseValue(nameof(input.AzureSkuName), input.AzureSkuName);

                    var r = await CreateInnerAsync(input, childLogger);
                    childLogger.FluentAddValue(nameof(r.AzureResourceInfo.Name), r.AzureResourceInfo.Name)
                            .FluentAddValue(nameof(r.RetryAfter), r.RetryAfter.ToString())
                            .FluentAddValue(nameof(r.NextInput.ContinuationToken), r.NextInput?.ContinuationToken)
                            .FluentAddValue(nameof(r.Status), r.Status.ToString());
                    return r;
                },
                (ex, childLogger) =>
                {
                    var result = new FileShareProviderCreateResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                    return Task.FromResult(result);
                },
                swallowException: true);
        }

        /// <inheritdoc/>
        public Task<FileShareProviderDeleteResult> DeleteAsync(
            FileShareProviderDeleteInput input,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                "file_share_storage_provider_delete_step",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(nameof(input.AzureResourceInfo.Name), input.AzureResourceInfo.Name);

                    var blobInput = input as FileShareProviderDeleteBlobInput;

                    childLogger.FluentAddValue("DeleteIsBlob", blobInput != null);

                    // When we are targeting a blob, trigger blob delete
                    if (blobInput != null)
                    {
                        await providerHelper.DeleteBlobContainerAsync(
                            blobInput.AzureResourceInfo,
                            blobInput.StorageAccountKey,
                            blobInput.BlobContainerName,
                            childLogger);
                    }
                    else
                    {
                        await providerHelper.DeleteStorageAccountAsync(
                            input.AzureResourceInfo,
                            childLogger);
                    }

                    var r = new FileShareProviderDeleteResult() { Status = OperationState.Succeeded };
                    childLogger.FluentAddValue(nameof(r.RetryAfter), r.RetryAfter.ToString())
                          .FluentAddValue(nameof(r.Status), r.Status.ToString());
                    return r;
                },
                (ex, childLogger) =>
                {
                    var result = new FileShareProviderDeleteResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                    return Task.FromResult(result);
                },
                swallowException: true);
        }

        /// <inheritdoc/>
        public Task<FileShareProviderAssignResult> StartAsync(
            FileShareProviderAssignInput input,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                "file_share_storage_provider_assign_step",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(nameof(input.AzureResourceInfo.Name), input.AzureResourceInfo.Name);

                    var info = await providerHelper.GetConnectionInfoAsync(input.AzureResourceInfo, input.StorageType, childLogger);
                    var r = new FileShareProviderAssignResult(
                        info.StorageAccountName,
                        info.StorageAccountKey,
                        info.StorageShareName,
                        info.StorageFileName,
                        info.StorageFileServiceHost)
                    { Status = OperationState.Succeeded };
                    childLogger.FluentAddValue(nameof(r.RetryAfter), r.RetryAfter.ToString())
                        .FluentAddValue(nameof(r.Status), r.Status.ToString());
                    return r;
                },
                (ex, childLogger) =>
                {
                    var result = new FileShareProviderAssignResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                    return Task.FromResult(result);
                },
                swallowException: true);
        }

        /// <inheritdoc/>
        public Task<FileShareProviderArchiveResult> ArchiveAsync(
            FileShareProviderArchiveInput input,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                "file_share_storage_provider_archive_step",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(nameof(input.SrcAzureResourceInfo.Name), input.SrcAzureResourceInfo.Name);

                    var result = await ArchiveInnerAsync(input, childLogger);
                    childLogger.FluentAddValue(nameof(result.AzureResourceInfo.Name), result.AzureResourceInfo.Name)
                            .FluentAddValue(nameof(result.RetryAfter), result.RetryAfter.ToString())
                            .FluentAddValue(nameof(result.NextInput.ContinuationToken), result.NextInput?.ContinuationToken)
                            .FluentAddValue(nameof(result.Status), result.Status.ToString());
                    return result;
                },
                (ex, childLogger) =>
                {
                    var result = new FileShareProviderArchiveResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                    return Task.FromResult(result);
                },
                swallowException: true);
        }

        /// <summary>
        /// Create operation helper function that implements the Create continuation state machine.
        /// </summary>
        /// <param name="input">Provides input to Create Azure file share.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>
        /// The result of this step in the state machine.
        /// </returns>
        private async Task<FileShareProviderCreateResult> CreateInnerAsync(
            FileShareProviderCreateInput input,
            IDiagnosticsLogger logger)
        {
            TimeSpan resultRetryAfter = default;
            string resultContinuationToken = default;
            FileShareProviderCreateState nextState;
            AzureResourceInfo resultResourceInfo;
            BatchTaskInfo prepareTaskInfo = default;
            var continuationToken = input.ContinuationToken;

            if (continuationToken == null)
            {
                var resourceInfo = await providerHelper.CreateStorageAccountAsync(
                    input.AzureSubscription,
                    input.AzureLocation,
                    input.AzureResourceGroup,
                    input.AzureSkuName,
                    input.ResourceTags,
                    logger);
                resultResourceInfo = resourceInfo;
                nextState = FileShareProviderCreateState.CreateFileShare;
            }
            else
            {
                var prevContinuation = JsonConvert.DeserializeObject<FileShareProviderCreateContinuationToken>(continuationToken);
                switch (prevContinuation.NextState)
                {
                    case FileShareProviderCreateState.CreateFileShare:
                        await providerHelper.CreateFileShareAsync(prevContinuation.AzureResourceInfo, logger);
                        nextState = FileShareProviderCreateState.PrepareFileShare;
                        break;
                    case FileShareProviderCreateState.PrepareFileShare:
                        prepareTaskInfo = await batchPrepareFileShareJobProvider.StartPrepareFileShareAsync(prevContinuation.AzureResourceInfo, input.StorageCopyItems, input.StorageSizeInGb, logger);
                        nextState = FileShareProviderCreateState.CheckFileShare;
                        break;
                    case FileShareProviderCreateState.CheckFileShare:
                        var taskMaxWaitTime = TimeSpan.FromMinutes(30);
                        var prepareStatus = await batchPrepareFileShareJobProvider.CheckBatchTaskStatusAsync(prevContinuation.AzureResourceInfo, prevContinuation.PrepareTaskInfo, taskMaxWaitTime, logger);
                        if (prepareStatus == BatchTaskStatus.Succeeded)
                        {
                            nextState = default;
                        }
                        else if (prepareStatus == BatchTaskStatus.Failed)
                        {
                            throw new StorageCreateException("Failed to prepare the file share.");
                        }
                        else
                        {
                            resultRetryAfter = TimeSpan.FromSeconds(StorageCreationRetryAfterSeconds);
                            nextState = FileShareProviderCreateState.CheckFileShare;
                            prepareTaskInfo = prevContinuation.PrepareTaskInfo;
                        }

                        break;
                    default:
                        throw new StorageCreateException(string.Format("Invalid continuation token: {0}", continuationToken));
                }

                resultResourceInfo = prevContinuation.AzureResourceInfo;
            }

            OperationState resultState;
            if (nextState == default)
            {
                resultState = OperationState.Succeeded;
            }
            else
            {
                var nextContinuation = new FileShareProviderCreateContinuationToken(nextState, resultResourceInfo, prepareTaskInfo);
                resultContinuationToken = JsonConvert.SerializeObject(nextContinuation);
                resultState = OperationState.InProgress;
            }

            var result = new FileShareProviderCreateResult()
            {
                AzureResourceInfo = resultResourceInfo,
                RetryAfter = resultRetryAfter,
                NextInput = input.BuildNextInput(resultContinuationToken),
                Status = resultState,
            };

            return result;
        }

        /// <summary>
        /// Suspend operation helper function that implements the Suspend continuation state machine.
        /// </summary>
        /// <param name="input">Provides input to Create Azure file share.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>
        /// The result of this step in the state machine.
        /// </returns>
        private async Task<FileShareProviderArchiveResult> ArchiveInnerAsync(
            FileShareProviderArchiveInput input,
            IDiagnosticsLogger logger)
        {
            TimeSpan resultRetryAfter = default;
            string resultContinuationToken = default;
            FileShareProviderArchiveState nextState;
            AzureResourceInfo resultResourceInfo;
            BatchTaskInfo archiveTaskInfo = default;

            var continuationToken = input.ContinuationToken;
            if (continuationToken == null)
            {
                archiveTaskInfo = await batchArchiveFileShareJobProvider.StartArchiveFileShareAsync(
                    input.SrcAzureResourceInfo, input.SrcFileShareUriWithSas, input.DestBlobUriWithSas, logger);
                nextState = FileShareProviderArchiveState.CheckBlob;
                resultResourceInfo = input.SrcAzureResourceInfo;
            }
            else
            {
                var prevContinuation = JsonConvert.DeserializeObject<FileShareProviderArchiveContinuationToken>(continuationToken);
                var taskMaxWaitTime = default(TimeSpan);
                var prepareStatus = await batchArchiveFileShareJobProvider.CheckBatchTaskStatusAsync(prevContinuation.AzureResourceInfo, prevContinuation.PrepareTaskInfo, taskMaxWaitTime, logger);
                if (prepareStatus == BatchTaskStatus.Succeeded)
                {
                    nextState = default;
                }
                else if (prepareStatus == BatchTaskStatus.Failed)
                {
                    throw new StorageCreateException("Failed to archive the file share.");
                }
                else
                {
                    resultRetryAfter = TimeSpan.FromSeconds(StorageCreationRetryAfterSeconds);
                    nextState = FileShareProviderArchiveState.CheckBlob;
                    archiveTaskInfo = prevContinuation.PrepareTaskInfo;
                }

                resultResourceInfo = prevContinuation.AzureResourceInfo;
            }

            OperationState resultState;
            if (nextState == default)
            {
                resultState = OperationState.Succeeded;
            }
            else
            {
                var nextContinuation = new FileShareProviderArchiveContinuationToken(nextState, resultResourceInfo, archiveTaskInfo);
                resultContinuationToken = JsonConvert.SerializeObject(nextContinuation);
                resultState = OperationState.InProgress;
            }

            var result = new FileShareProviderArchiveResult()
            {
                AzureResourceInfo = resultResourceInfo,
                RetryAfter = resultRetryAfter,
                NextInput = input.BuildNextInput(resultContinuationToken),
                Status = resultState,
            };

            return result;
        }
    }
}
