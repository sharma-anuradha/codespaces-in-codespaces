// <copyright file="StartEnvironmentResultHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring.DataHandlers
{
    /// <summary>
    /// Process start environment job result.
    /// </summary>
    public class StartEnvironmentResultHandler : IDataHandler
    {
        private readonly IEnvironmentManager environmentManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartEnvironmentResultHandler"/> class.
        /// </summary>
        /// <param name="environmentManager">Environment Manager.</param>
        public StartEnvironmentResultHandler(
            IEnvironmentManager environmentManager)
        {
            this.environmentManager = environmentManager;
        }

        /// <inheritdoc/>
        public bool CanProcess(CollectedData data)
        {
            return data is JobResult && data.Name == JobCommand.StartEnvironment.ToString();
        }

        /// <inheritdoc/>
        public Task<CollectedDataHandlerContext> ProcessAsync(CollectedData data, CollectedDataHandlerContext handlerContext, Guid vmResourceId, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "start_environment_handler_process",
                async (childLogger) =>
                {
                    if (!CanProcess(data))
                    {
                        throw new InvalidOperationException($"Collected data of type {data?.GetType().Name}, name  {data?.Name} cannot be processed by {nameof(StartEnvironmentResultHandler)}.");
                    }

                    var jobResultData = (JobResult)data;
                    var environmentTransition = handlerContext.CloudEnvironmentTransition;

                    childLogger.FluentAddBaseValue("CloudEnvironmentId", jobResultData.EnvironmentId)
                        .FluentAddValue("ComputeResourceId", vmResourceId)
                        .FluentAddValue("CloudEnvironmentFound", environmentTransition?.Value != null)
                        .FluentAddValue("JobCollectedData", JsonConvert.SerializeObject(jobResultData))
                        .FluentAddValue("JobState", jobResultData.JobState);

                    ValidationUtil.IsRequired(jobResultData.EnvironmentId, nameof(jobResultData.EnvironmentId));

                    if (environmentTransition?.Value == null)
                    {
                        return handlerContext;
                    }

                    if (jobResultData.JobState == JobState.Succeeded)
                    {
                        // Only call resume callback when we are calling back from a resume
                        if (environmentTransition.Value.State == CloudEnvironmentState.Starting)
                        {
                            // Extract mount file share result
                            var payloadStageResult = jobResultData.OperationResults.Where(x => x.Name == "PayloadStage").SingleOrDefault();

                            logger.FluentAddValue("JobFoundMountFileShareReult", payloadStageResult != null);

                            // Bail if we didn't find the result
                            if (payloadStageResult == null)
                            {
                                throw new ArgumentNullException("Expected mount file share result was not found.");
                            }

                            // Validate that we have needed data
                            var computeResourceId = payloadStageResult.Data.GetValueOrDefault("ComputeResourceId");
                            var storageResourceId = payloadStageResult.Data.GetValueOrDefault("StorageResourceId");
                            var archiveStorageResourceId = payloadStageResult.Data.GetValueOrDefault("StorageArchiveResourceId");

                            logger.FluentAddBaseValue("ComputeResourceId", computeResourceId)
                                .FluentAddBaseValue("StorageResourceId", storageResourceId)
                                .FluentAddBaseValue("ArchiveStorageResourceId", archiveStorageResourceId);

                            // Update environment to finalized state
                            // Call only if storageResourceId is a valid Guid.
                            if (Guid.TryParse(storageResourceId, out var storageResourceIdGuid))
                            {
                                var environment = await environmentManager.ResumeCallbackAsync(
                                    Guid.Parse(environmentTransition.Value.Id),
                                    storageResourceIdGuid,
                                    string.IsNullOrEmpty(archiveStorageResourceId) ? default(Guid?) : Guid.Parse(archiveStorageResourceId),
                                    childLogger.NewChildLogger());

                                // replace environment record and add tranistions.
                                await environmentTransition.ReplaceAndReplayTransitionsAsync(environment);
                            }
                        }
                    }
                    else if (jobResultData.JobState == JobState.Failed)
                    {
                        // TODO :: Call FailEnvironmentWorkflow to fail and cleanup resources.
                        // Mark environment provision to failed status
                        if (environmentTransition.Value.State == CloudEnvironmentState.Provisioning)
                        {
                            var newState = CloudEnvironmentState.Failed;
                            var errorMessage = MessageCodeUtils.GetCodeFromError(jobResultData.Errors) ?? MessageCodes.StartEnvironmentGenericError.ToString();
                            handlerContext.CloudEnvironmentState = newState;
                            handlerContext.Reason = errorMessage;
                            handlerContext.Trigger = CloudEnvironmentStateUpdateTriggers.StartEnvironmentJobFailed;
                            handlerContext.IsUserError = jobResultData.JobErrorType == JobErrorType.User;

                            return handlerContext;
                        }
                        else if (environmentTransition.Value.State == CloudEnvironmentState.Starting)
                        {
                            // Shutdown the environment if the environment has failed to start.
                            var environment = await environmentManager.SuspendAsync(Guid.Parse(environmentTransition.Value.Id), false, childLogger.NewChildLogger());

                            // Reset environment record and tranistions.
                            environmentTransition.ReplaceAndResetTransition(default);

                            return new CollectedDataHandlerContext(environmentTransition) { StopProcessing = true };
                        }
                    }
                    else if (jobResultData.JobState == JobState.Started)
                    {
                        if (environmentTransition.Value.State == CloudEnvironmentState.Provisioning)
                        {
                            // begin transition monitoring if a timeout was specified.
                            if (jobResultData.Timeout.HasValue)
                            {
                                // update the environment's state timeout to the one provided by the agent.
                                environmentTransition.PushTransition(
                                    (env) =>
                                    {
                                        env.StateTimeout = jobResultData.Timeout;
                                    });
                            }
                        }
                    }

                    return handlerContext;
                });
        }
    }
}
