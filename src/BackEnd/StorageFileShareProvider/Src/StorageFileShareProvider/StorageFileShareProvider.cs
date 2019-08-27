// <copyright file="StorageFileShareProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
{
    /// <summary>
    /// Implementation of <see cref="IStorageProvider"/>.
    /// </summary>
    public class StorageFileShareProvider : IStorageProvider
    {
        private readonly IStorageFileShareProviderHelper providerHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageFileShareProvider"/> class.
        /// This class acts as the provider interface for File Share Storage.
        /// </summary>
        /// <param name="providerHelper">An implementation of the <see cref="IStorageFileShareProviderHelper"/> interface.</param>
        public StorageFileShareProvider(IStorageFileShareProviderHelper providerHelper)
        {
            this.providerHelper = Requires.NotNull(providerHelper, nameof(providerHelper));
        }

        /// <inheritdoc/>
        public async Task<FileShareProviderCreateResult> CreateAsync(
            FileShareProviderCreateInput input,
            IDiagnosticsLogger logger,
            string continuationToken = null)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            TimeSpan resultRetryAfter = default;
            string resultContinuationToken = default;

            FileShareProviderCreateState nextState;
            string resultResourceId;

            var duration = logger.StartDuration();

            logger = logger.WithValues(new LogValueSet
            {
                { nameof(input.AzureSubscription), input.AzureSubscription },
                { nameof(input.AzureLocation), input.AzureLocation },
                { nameof(input.AzureResourceGroup), input.AzureResourceGroup },
                { nameof(input.AzureSkuName), input.AzureSkuName },
            });

            if (continuationToken == null)
            {
                var storageAccountId = await providerHelper.CreateStorageAccountAsync(
                    input.AzureSubscription,
                    input.AzureLocation,
                    input.AzureResourceGroup,
                    logger);
                resultResourceId = storageAccountId;
                nextState = FileShareProviderCreateState.CreateFileShare;
            }
            else
            {
                var prevContinuation = JsonConvert.DeserializeObject<FileShareProviderCreateContinuationToken>(continuationToken);
                switch (prevContinuation.NextState)
                {
                    case FileShareProviderCreateState.CreateFileShare:
                        await providerHelper.CreateFileShareAsync(prevContinuation.ResourceId, logger);
                        nextState = FileShareProviderCreateState.PrepareFileShare;
                        break;
                    case FileShareProviderCreateState.PrepareFileShare:
                        await providerHelper.StartPrepareFileShareAsync(prevContinuation.ResourceId, input.StorageBlobUrl, logger);
                        nextState = FileShareProviderCreateState.CheckFileShare;
                        break;
                    case FileShareProviderCreateState.CheckFileShare:
                        var completed = await providerHelper.CheckPrepareFileShareAsync(prevContinuation.ResourceId, logger);
                        if (completed == 1)
                        {
                            nextState = default;
                        }
                        else
                        {
                            resultRetryAfter = TimeSpan.FromMinutes(1);
                            nextState = FileShareProviderCreateState.CheckFileShare;
                        }
                        break;
                    default:
                        throw new StorageCreateException(string.Format("Invalid continuation token: {0}", continuationToken));
                }

                resultResourceId = prevContinuation.ResourceId;
            }

            var resultState = OperationState.Succeeded;
            if (nextState != default)
            {
                var nextContinuation = new FileShareProviderCreateContinuationToken(nextState, resultResourceId);
                resultContinuationToken = JsonConvert.SerializeObject(nextContinuation);
                resultState = OperationState.InProgress;
            }

            var result = new FileShareProviderCreateResult()
            {
                ResourceId = resultResourceId,
                RetryAfter = resultRetryAfter,
                ContinuationToken = resultContinuationToken,
                Status = resultState,
            };

            logger
                .FluentAddValue(nameof(result.ResourceId), result.ResourceId)
                .FluentAddValue(nameof(result.RetryAfter), result.RetryAfter.ToString())
                .FluentAddValue(nameof(result.ContinuationToken), result.ContinuationToken)
                .FluentAddValue(nameof(result.Status), result.Status.ToString())
                .AddDuration(duration)
                .LogInfo("file_share_storage_provider_create_step_complete");

            return result;
        }

        /// <inheritdoc/>
        public async Task<FileShareProviderDeleteResult> DeleteAsync(
            FileShareProviderDeleteInput input,
            IDiagnosticsLogger logger,
            string continuationToken = null)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            var duration = logger.StartDuration();

            logger = logger.WithValue(nameof(input.ResourceId), input.ResourceId);

            await providerHelper.DeleteStorageAccountAsync(input.ResourceId, logger);

            var result = new FileShareProviderDeleteResult() { Status = OperationState.Succeeded };

            logger
                .FluentAddValue(nameof(input.ResourceId), input.ResourceId)
                .FluentAddValue(nameof(result.RetryAfter), result.RetryAfter.ToString())
                .FluentAddValue(nameof(result.ContinuationToken), result.ContinuationToken)
                .FluentAddValue(nameof(result.Status), result.Status.ToString())
                .AddDuration(duration)
                .LogInfo("file_share_storage_provider_delete_step_complete");

            return result;
        }

        /// <inheritdoc/>
        public async Task<FileShareProviderAssignResult> AssignAsync(
            FileShareProviderAssignInput input,
            IDiagnosticsLogger logger,
            string continuationToken = null)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            var duration = logger.StartDuration();

            logger = logger.WithValue(nameof(input.ResourceId), input.ResourceId);

            var info = await providerHelper.GetConnectionInfoAsync(input.ResourceId, logger);

            var result = new FileShareProviderAssignResult(
                info.StorageAccountName,
                info.StorageAccountKey,
                info.StorageShareName,
                info.StorageFileName)
            { Status = OperationState.Succeeded };

            logger
                .FluentAddValue(nameof(input.ResourceId), input.ResourceId)
                .FluentAddValue(nameof(result.RetryAfter), result.RetryAfter.ToString())
                .FluentAddValue(nameof(result.ContinuationToken), result.ContinuationToken)
                .FluentAddValue(nameof(result.Status), result.Status.ToString())
                .AddDuration(duration)
                .LogInfo("file_share_storage_provider_assign_step_complete");

            return result;
        }
    }
}
