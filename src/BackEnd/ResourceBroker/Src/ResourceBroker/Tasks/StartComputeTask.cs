// <copyright file="StartComputeTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using AutoMapper;
using Hangfire;
using Hangfire.States;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task which starts compute resource.
    /// </summary>
    public class StartComputeTask : IStartComputeTask
    {
        public const string QueueName = "start-compute-job-queue";

        public StartComputeTask(
            IResourceRepository resourceRepository,
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IBackgroundJobClient backgroundJobs,
            IMapper mapper)
        {
            ResourceRepository = resourceRepository;
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
            BackgroundJobs = backgroundJobs;
            Mapper = mapper;
            EnqueuedState = new EnqueuedState
            {
                Queue = QueueName,
            };
        }

        private IResourceRepository ResourceRepository { get; }

        private IComputeProvider ComputeProvider { get; }

        private IStorageProvider StorageProvider { get; }

        private IBackgroundJobClient BackgroundJobs { get; }

        private IMapper Mapper { get; }

        private EnqueuedState EnqueuedState { get; }

        /// <inheritdoc/>
        public async Task<EnvironmentStartResult> RunAsync(
            EnvironmentStartInput input,
            IDiagnosticsLogger logger,
            string continuationToken = null)
        {
            if (string.IsNullOrEmpty(continuationToken))
            {
                return await RunInitializeAsync(input, logger);
            }

            return await StatusCheckAsync(input, logger, continuationToken);
        }

        public async Task<EnvironmentStartResult> RunInitializeAsync(
            EnvironmentStartInput input,
            IDiagnosticsLogger logger)
        {
            var result = new EnvironmentStartResult
            {
                ResourceId = input.ComputeResourceId,
                Status = ResourceStartingStatus.Initialized.ToString(),
                ContinuationToken = input.ComputeResourceId.ToString(),
                RetryAfter = TimeSpan.FromSeconds(10),
            };

            // Queue actual work so we can return asap
            BackgroundJobs.Create(() => RunStartAsync(input, logger), EnqueuedState);

            return result;
        }

        public async Task<EnvironmentStartResult> RunStartAsync(
            EnvironmentStartInput input,
            IDiagnosticsLogger logger)
        {
            // Get file share connection info for target share
            var fileShareProviderAssignInput = new FileShareProviderAssignInput { ResourceId = input.StorageResourceId };
            var storageResult = await StorageProvider.AssignAsync(fileShareProviderAssignInput, null)
                .ContinueOnAnyContext();

            // Start compute preperation process
            var computeStorageFileShareInfo = Mapper.Map<ShareConnectionInfo>(storageResult);
            var computeProviderStartInput = new VirtualMachineProviderStartComputeInput(
                input.ComputeResourceId,
                computeStorageFileShareInfo,
                input.EnvironmentVariables);

            // Run core task
            var computeResult = await RunContinuationAsync(computeProviderStartInput, logger, null);

            // Setup result to send back
            var result = Mapper.Map<EnvironmentStartResult>(computeResult);
            result.ContinuationToken = input.ComputeResourceId.InstanceId.ToString();

            return result;
        }

        [Queue(QueueName)]
        public async Task<VirtualMachineProviderStartComputeResult> RunContinuationAsync(
            VirtualMachineProviderStartComputeInput computeProviderStartInput,
            IDiagnosticsLogger logger,
            string continuationToken = null)
        {
            // Continue compute continuation
            var computeResult = await ComputeProvider.StartComputeAsync(computeProviderStartInput, continuationToken);

            // Get the resource so that we can update the status
            var resource = await ResourceRepository.GetAsync(computeProviderStartInput.ResourceId.InstanceId.ToString(), logger);

            // TODO: Should have a null check here

            // Compute the current status
            var startingStatus = string.IsNullOrEmpty(computeResult.ContinuationToken) ?
                ResourceStartingStatus.Complete :
                (resource.StartingStatus.HasValue ?
                    ResourceStartingStatus.Waiting : ResourceStartingStatus.Initialized);

            // Update the resource if needed
            if (!resource.StartingStatus.HasValue
                || resource.StartingStatus.Value != startingStatus)
            {
                resource.UpdateStartingStatus(startingStatus);

                await ResourceRepository.UpdateAsync(resource, logger);
            }

            // Queue next status check task if needed
            if (startingStatus != ResourceStartingStatus.Complete)
            {
                // Recursively call ourselves
                BackgroundJobs.Schedule(
                    () => RunContinuationAsync(computeProviderStartInput, logger, computeResult.ContinuationToken),
                    computeResult.RetryAfter);
            }

            return computeResult;
        }

        public async Task<EnvironmentStartResult> StatusCheckAsync(
            EnvironmentStartInput input,
            IDiagnosticsLogger logger,
            string continuationToken)
        {
            var resource = await ResourceRepository.GetAsync(input.ComputeResourceId.InstanceId.ToString(), logger);

            // TODO: Should have a null check here

            var result = new EnvironmentStartResult
            {
                ResourceId = input.ComputeResourceId,
                Status = resource.StartingStatus.ToString(),
                ContinuationToken = continuationToken,
                RetryAfter = TimeSpan.FromSeconds(10),
            };

            if (resource.StartingStatus != ResourceStartingStatus.Complete)
            {
                result.ContinuationToken = null;
                result.RetryAfter = default(TimeSpan);
            }

            return result;
        }
    }
}
