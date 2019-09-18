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
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            var duration = logger.StartDuration();

            logger = logger.WithValues(new LogValueSet
            {
                { nameof(input.AzureSubscription), input.AzureSubscription },
                { nameof(input.AzureLocation), input.AzureLocation },
                { nameof(input.AzureResourceGroup), input.AzureResourceGroup },
                { nameof(input.AzureSkuName), input.AzureSkuName },
            });

            var result = await logger.OperationScopeAsync(
                    "file_share_storage_provider_create_step",
                    async () =>
                    {
                        var r = await CreateInnerAsync(input, logger);
                        logger.FluentAddValue(nameof(r.AzureResourceInfo.Name), r.AzureResourceInfo.Name)
                              .FluentAddValue(nameof(r.RetryAfter), r.RetryAfter.ToString())
                              .FluentAddValue(nameof(r.NextInput.ContinuationToken), r.NextInput?.ContinuationToken)
                              .FluentAddValue(nameof(r.Status), r.Status.ToString());
                        return r;
                    },
                    (_) => new FileShareProviderCreateResult() { Status = OperationState.Failed },
                    swallowException: true);

            return result;
        }

        /// <inheritdoc/>
        public async Task<FileShareProviderDeleteResult> DeleteAsync(
            FileShareProviderDeleteInput input,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            var duration = logger.StartDuration();

            logger = logger.WithValue(nameof(input.AzureResourceInfo.Name), input.AzureResourceInfo.Name);

            var result = await logger.OperationScopeAsync(
                "file_share_storage_provider_delete_step",
                async () =>
                {
                    await providerHelper.DeleteStorageAccountAsync(input.AzureResourceInfo, logger);
                    var r = new FileShareProviderDeleteResult() { Status = OperationState.Succeeded };
                    logger.FluentAddValue(nameof(r.RetryAfter), r.RetryAfter.ToString())
                          .FluentAddValue(nameof(r.Status), r.Status.ToString());
                    return r;
                },
                (_) => new FileShareProviderDeleteResult() { Status = OperationState.Failed },
                swallowException: true);

            return result;
        }

        /// <inheritdoc/>
        public async Task<FileShareProviderAssignResult> AssignAsync(
            FileShareProviderAssignInput input,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            var duration = logger.StartDuration();

            logger = logger.WithValue(nameof(input.AzureResourceInfo.Name), input.AzureResourceInfo.Name);

            var result = await logger.OperationScopeAsync(
                "file_share_storage_provider_assign_step",
                async () =>
                {
                    var info = await providerHelper.GetConnectionInfoAsync(input.AzureResourceInfo, logger);
                    var r = new FileShareProviderAssignResult(
                        info.StorageAccountName,
                        info.StorageAccountKey,
                        info.StorageShareName,
                        info.StorageFileName)
                    { Status = OperationState.Succeeded };
                    logger.FluentAddValue(nameof(r.RetryAfter), r.RetryAfter.ToString())
                        .FluentAddValue(nameof(r.Status), r.Status.ToString())
                        .AddDuration(duration);
                    return r;
                },
                (_) => new FileShareProviderAssignResult() { Status = OperationState.Failed },
                swallowException: true);

            return result;
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
            string continuationToken = input.ContinuationToken;

            if (continuationToken == null)
            {
                var resourceInfo = await providerHelper.CreateStorageAccountAsync(
                    input.AzureSubscription,
                    input.AzureLocation,
                    input.AzureResourceGroup,
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
                        await providerHelper.StartPrepareFileShareAsync(prevContinuation.AzureResourceInfo, input.StorageBlobUrl, logger);
                        nextState = FileShareProviderCreateState.CheckFileShare;
                        break;
                    case FileShareProviderCreateState.CheckFileShare:
                        var completed = await providerHelper.CheckPrepareFileShareAsync(prevContinuation.AzureResourceInfo, logger);
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

                resultResourceInfo = prevContinuation.AzureResourceInfo;
            }

            OperationState resultState;
            if (nextState == default)
            {
                resultState = OperationState.Succeeded;
            }
            else
            {
                var nextContinuation = new FileShareProviderCreateContinuationToken(nextState, resultResourceInfo);
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
    }
}
