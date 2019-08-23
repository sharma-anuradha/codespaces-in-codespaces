// <copyright file="StartComputeTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using AutoMapper;
using Hangfire;
using Hangfire.States;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
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
        public const string QueueName = "start-compute-task";

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
            // Create base logger
            var timer = logger.StartDuration();
            logger = logger.FromExisting();

            try
            {
                var result = new EnvironmentStartResult
                {
                    ResourceId = input.ComputeResourceId,
                    Status = OperationState.Initialized,
                    ContinuationToken = input.ComputeResourceId.ToString(),
                    RetryAfter = TimeSpan.FromSeconds(10),
                };

                // Queue actual work so we can return asap
                BackgroundJobs.Create(() => RunStartAsync(input, logger, timer), EnqueuedState);

                logger.AddDuration(timer).LogInfo("start_compute_initialize_complete");

                return result;
            }
            catch (Exception e)
            {
                logger.AddDuration(timer).LogException("start_compute_initialize_error", e);

                throw;
            }
        }

        public async Task<EnvironmentStartResult> RunStartAsync(
            EnvironmentStartInput input,
            IDiagnosticsLogger logger,
            DiagnosticsLoggerExtensions.Duration timer)
        {
            try
            {
                logger.FluentAddValue("DurationStart", timer.Elapsed.TotalMilliseconds.ToString());

                // Get file share connection info for target share
                var fileShareProviderAssignInput = new FileShareProviderAssignInput { ResourceId = input.StorageResourceId };
                var storageResult = await StorageProvider.AssignAsync(fileShareProviderAssignInput, null)
                    .ContinueOnAnyContext();

                logger.FluentAddValue("DurationPostFileShareAssigned", timer.Elapsed.TotalMilliseconds.ToString());

                // Start compute preperation process
                var computeStorageFileShareInfo = Mapper.Map<ShareConnectionInfo>(storageResult);
                var computeProviderStartInput = new VirtualMachineProviderStartComputeInput(
                    input.ComputeResourceId,
                    computeStorageFileShareInfo,
                    input.EnvironmentVariables);

                // Run core task
                var computeResult = await RunContinuationAsync(computeProviderStartInput, logger, timer, null);

                logger.FluentAddValue("DurationPostInitialRunContinuation", timer.Elapsed.TotalMilliseconds.ToString());

                // Setup result to send back
                var result = Mapper.Map<EnvironmentStartResult>(computeResult);
                result.ContinuationToken = input.ComputeResourceId.InstanceId.ToString();

                logger.AddDuration(timer).LogInfo("start_compute_start_complete");

                return result;
            }
            catch (Exception e)
            {
                logger.AddDuration(timer).LogException("start_compute_start_error", e);

                throw;
            }
        }

        [Queue(QueueName)]
        public async Task<VirtualMachineProviderStartComputeResult> RunContinuationAsync(
            VirtualMachineProviderStartComputeInput computeProviderStartInput,
            IDiagnosticsLogger logger,
            DiagnosticsLoggerExtensions.Duration timer,
            string continuationToken = null)
        {
            try
            {
                logger
                    .FluentAddValue("IsInitialContinuation", string.IsNullOrEmpty(continuationToken).ToString())
                    .FluentAddValue("DurationStart", timer.Elapsed.TotalMilliseconds.ToString());

                // Continue compute continuation
                var computeResult = await ComputeProvider.StartComputeAsync(computeProviderStartInput, continuationToken);

                logger.FluentAddValue("DurationPostComputeStart", timer.Elapsed.TotalMilliseconds.ToString());

                // Get the resource so that we can update the status
                var resource = await ResourceRepository.GetAsync(computeProviderStartInput.ResourceId.InstanceId.ToString(), logger);

                logger.FluentAddValue("DurationPostFetchResource", timer.Elapsed.TotalMilliseconds.ToString());

                // Ensure that we have a record to work with
                if (resource == null)
                {
                    throw new ArgumentNullException("Was not able to find target compute resource.");
                }

                logger
                    .FluentAddValue(ResourceLoggingConstants.ResourceLocation, resource.Location)
                    .FluentAddValue(ResourceLoggingConstants.ResourceSkuName, resource.SkuName)
                    .FluentAddValue(ResourceLoggingConstants.ResourceType, resource.Type.ToString());

                // Compute the current status
                // TODO:: Add handling for Failed / Canceled states
                var startingStatus = computeResult.Status;

                logger.FluentAddValue("StartingStatus", startingStatus.ToString());

                // Update the resource if needed
                if (!resource.StartingStatus.HasValue
                    || resource.StartingStatus.Value != startingStatus)
                {
                    logger.FluentAddValue("DidStatusUpdate", "true");

                    // Update the Status
                    resource.UpdateStartingStatus(startingStatus);

                    // Update database record
                    await ResourceRepository.UpdateAsync(resource, logger);

                    logger.FluentAddValue("DurationPostStatusUpdate", timer.Elapsed.TotalMilliseconds.ToString());
                }

                // Queue next status check task if needed
                if (startingStatus != OperationState.Succeeded)
                {
                    logger.FluentAddValue("DidTriggerNextContinuation", "true");

                    // Recursively call ourselves
                    BackgroundJobs.Schedule(
                        () => RunContinuationAsync(computeProviderStartInput, logger, timer, computeResult.ContinuationToken),
                        computeResult.RetryAfter);
                }

                logger.AddDuration(timer).LogInfo("start_compute_continuation_complete");

                return computeResult;
            }
            catch (Exception e)
            {
                logger.AddDuration(timer).LogException("start_compute_continuation_error", e);

                throw;
            }
        }

        public async Task<EnvironmentStartResult> StatusCheckAsync(
            EnvironmentStartInput input,
            IDiagnosticsLogger logger,
            string continuationToken)
        {
            // Find target resource
            var resource = await ResourceRepository.GetAsync(input.ComputeResourceId.InstanceId.ToString(), logger);

            // Ensure that we have a record to work with
            if (resource == null)
            {
                throw new ArgumentNullException("Was not able to find target compute resource.");
            }

            // Build result to return
            var result = new EnvironmentStartResult
            {
                ResourceId = input.ComputeResourceId,
                Status = resource.StartingStatus.HasValue ? OperationState.NotStarted : resource.StartingStatus.Value,
                ContinuationToken = continuationToken,
                RetryAfter = TimeSpan.FromSeconds(10),
            };
            if (resource.StartingStatus != OperationState.Succeeded)
            {
                result.ContinuationToken = null;
                result.RetryAfter = default(TimeSpan);
            }

            return result;
        }
    }
}
