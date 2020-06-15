// <copyright file="DiskProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider
{
    /// <summary>
    /// Implements Disk provider for Azure virtual machines.
    /// </summary>
    public class DiskProvider : IDiskProvider
    {
        private const int MaxRetryAttempts = 30;

        private const int SecondsUntilNextRetry = 10;

        private const int SecondsToNextStep = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskProvider"/> class.
        /// </summary>
        /// <param name="clientFactory">Azure client factory.</param>
        /// <param name="queueProvider">Queue provider.</param>
        public DiskProvider(
            IAzureClientFactory clientFactory,
            IQueueProvider queueProvider)
        {
            ClientFactory = Requires.NotNull(clientFactory, nameof(clientFactory));
            QueueProvider = Requires.NotNull(queueProvider, nameof(queueProvider));
        }

        private IAzureClientFactory ClientFactory { get; }

        private IQueueProvider QueueProvider { get; }

        /// <inheritdoc/>
        public Task<DiskProviderDeleteResult> DeleteDiskAsync(
            DiskProviderDeleteInput input,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                "disk_provider_delete",
                async (childLogger) =>
                {
                    // TODO: janraj, logging.
                    var continuation = GetContinuationObject(input);

                    if (continuation.RetryAttempt > MaxRetryAttempts)
                    {
                        return new DiskProviderDeleteResult()
                        {
                            ErrorReason = "Failed. maximum retry attempts reached.",
                            Status = OperationState.Failed,
                        };
                    }

                    switch (continuation.NextState)
                    {
                        case DiskProviderDeleteState.CheckAttachedDisk:
                            return await CheckAttachedDiskStateAsync(input, continuation);

                        case DiskProviderDeleteState.BeginDeleteDisk:
                            return await BeginDeleteDiskAsync(input, continuation, childLogger);

                        case DiskProviderDeleteState.CheckDeletedDiskState:
                            return await CheckDeletedDiskStateAsync(input, continuation);

                        default:
                            throw new InvalidOperationException($"Invalid state. '{continuation.NextState}'");
                    }
                });
        }

        /// <inheritdoc/>
        public async Task<DiskProviderAcquireOSDiskResult> AcquireOSDiskAsync(
            DiskProviderAcquireOSDiskInput input,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return await logger.OperationScopeAsync(
                "disk_provider_get",
                async (childLogger) =>
                {
                    var azure = await ClientFactory.GetAzureClientAsync(input.VirtualMachineResourceInfo.SubscriptionId);
                    var resourceGroup = input.VirtualMachineResourceInfo.ResourceGroup;
                    var virtualMachine = await azure.VirtualMachines.GetByResourceGroupAsync(resourceGroup, input.VirtualMachineResourceInfo.Name);
                    var disk = await virtualMachine.Manager.Disks.GetByIdAsync(virtualMachine.OSDiskId);

                    var resourceTags = input.OSDiskResourceTags;
                    resourceTags[ResourceTagName.ResourceName] = disk.Name;

                    // Updates resource tags for orphan tracking.
                    await disk.Update()
                            .WithTags(resourceTags)
                            .ApplyAsync();

                    return new DiskProviderAcquireOSDiskResult()
                    {
                        AzureResourceInfo = new AzureResourceInfo()
                        {
                            Name = disk.Name,
                            ResourceGroup = resourceGroup,
                            SubscriptionId = input.VirtualMachineResourceInfo.SubscriptionId,
                        },
                    };
                });
        }

        /// <inheritdoc/>
        public async Task<bool> IsDetachedAsync(AzureResourceInfo azureResourceInfo, IDiagnosticsLogger logger)
        {
            var azure = await ClientFactory.GetAzureClientAsync(azureResourceInfo.SubscriptionId);
            var disk = await azure.Disks.GetByResourceGroupAsync(azureResourceInfo.ResourceGroup, azureResourceInfo.Name);
            return !disk.IsAttachedToVirtualMachine;
        }

        private static DiskProviderDeleteContinuationToken GetContinuationObject(DiskProviderDeleteInput input)
        {
            var continuationToken = input.ContinuationToken;
            if (continuationToken == default)
            {
                return new DiskProviderDeleteContinuationToken(
                    input.AzureResourceInfo,
                    input.QueueResourceInfo,
                    DiskProviderDeleteState.CheckAttachedDisk);
            }
            else
            {
                return JsonConvert.DeserializeObject<DiskProviderDeleteContinuationToken>(continuationToken);
            }
        }

        private static DiskProviderDeleteInput GetNextInputs(DiskProviderDeleteContinuationToken continuation, DiskProviderDeleteState nextState, int retryAttempt)
        {
            var nextContinuation = new DiskProviderDeleteContinuationToken(
                continuation.AzureResourceInfo,
                continuation.QueueAzureResourceInfo,
                nextState,
                retryAttempt);

            return new DiskProviderDeleteInput()
            {
                AzureResourceInfo = continuation.AzureResourceInfo,
                QueueResourceInfo = continuation.QueueAzureResourceInfo,
                ContinuationToken = JsonConvert.SerializeObject(nextContinuation),
            };
        }

        private async Task<DiskProviderDeleteResult> CheckAttachedDiskStateAsync(
            DiskProviderDeleteInput input,
            DiskProviderDeleteContinuationToken continuation)
        {
            var disk = await GetDiskAsync(input);

            if (disk == default)
            {
                // Nothing to do.
                return new DiskProviderDeleteResult() { Status = OperationState.Succeeded };
            }

            if (disk.IsAttachedToVirtualMachine)
            {
                return new DiskProviderDeleteResult()
                {
                    RetryAfter = TimeSpan.FromSeconds(SecondsUntilNextRetry),
                    Status = OperationState.InProgress,
                    NextInput = GetNextInputs(continuation, DiskProviderDeleteState.CheckAttachedDisk, continuation.RetryAttempt + 1),
                };
            }
            else
            {
                return new DiskProviderDeleteResult()
                {
                    RetryAfter = TimeSpan.FromSeconds(SecondsToNextStep),
                    Status = OperationState.InProgress,
                    NextInput = GetNextInputs(continuation, DiskProviderDeleteState.BeginDeleteDisk, continuation.RetryAttempt),
                };
            }
        }

        private async Task<DiskProviderDeleteResult> CheckDeletedDiskStateAsync(DiskProviderDeleteInput input, DiskProviderDeleteContinuationToken continuation)
        {
            var disk = await GetDiskAsync(input);

            if (disk == default)
            {
                // Deleted.
                return new DiskProviderDeleteResult() { Status = OperationState.Succeeded };
            }
            else
            {
                return new DiskProviderDeleteResult()
                {
                    RetryAfter = TimeSpan.FromSeconds(SecondsUntilNextRetry),
                    Status = OperationState.InProgress,
                    NextInput = GetNextInputs(continuation, DiskProviderDeleteState.CheckDeletedDiskState, continuation.RetryAttempt + 1),
                };
            }
        }

        private async Task<IDisk> GetDiskAsync(DiskProviderDeleteInput input)
        {
            var azure = await ClientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
            var resourceGroup = input.AzureResourceInfo.ResourceGroup;
            var diskName = input.AzureResourceInfo.Name;
            return await azure.Disks.GetByResourceGroupAsync(resourceGroup, diskName);
        }

        private async Task<DiskProviderDeleteResult> BeginDeleteDiskAsync(DiskProviderDeleteInput input, DiskProviderDeleteContinuationToken continuation, IDiagnosticsLogger logger)
        {
            await DeleteQueueComponent(input, logger);

            var resourceGroup = input.AzureResourceInfo.ResourceGroup;
            var diskName = input.AzureResourceInfo.Name;
            var computeClient = await ClientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);
            await computeClient.Disks.BeginDeleteAsync(resourceGroup, diskName);

            return new DiskProviderDeleteResult()
            {
                RetryAfter = TimeSpan.FromSeconds(SecondsToNextStep),
                Status = OperationState.InProgress,
                NextInput = GetNextInputs(continuation, DiskProviderDeleteState.CheckDeletedDiskState, continuation.RetryAttempt),
            };
        }

        private async Task DeleteQueueComponent(DiskProviderDeleteInput input, IDiagnosticsLogger logger)
        {
            if (input.QueueResourceInfo != default)
            {
                var queueDeleteInfo = new QueueProviderDeleteInput()
                {
                    AzureResourceInfo = input.QueueResourceInfo,
                };

                await QueueProvider.DeleteAsync(queueDeleteInfo, logger.NewChildLogger());
            }
        }
    }
}
