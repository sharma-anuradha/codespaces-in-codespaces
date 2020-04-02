// <copyright file="StartEnvironmentResultHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Management.Monitor.Fluent.AutoscaleSetting.Definition;
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
        public StartEnvironmentResultHandler(IEnvironmentManager environmentManager)
        {
            this.environmentManager = environmentManager;
        }

        /// <inheritdoc/>
        public bool CanProcess(CollectedData data)
        {
            return data is JobResult && data.Name == JobCommand.StartEnvironment.ToString();
        }

        /// <inheritdoc/>
        public async Task<CollectedDataHandlerContext> ProcessAsync(CollectedData data, CollectedDataHandlerContext handlerContext, Guid vmResourceId, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                "start_environment_handler_process",
                async (childLogger) =>
                {
                    if (!CanProcess(data))
                    {
                        throw new InvalidOperationException($"Collected data of type {data?.GetType().Name}, name  {data?.Name} cannot be processed by {nameof(StartEnvironmentResultHandler)}.");
                    }

                    var jobResultData = (JobResult)data;
                    var cloudEnvironment = handlerContext.CloudEnvironment;

                    childLogger.FluentAddBaseValue("CloudEnvironmentId", jobResultData.EnvironmentId)
                        .FluentAddValue("ComputeResourceId", vmResourceId)
                        .FluentAddValue("CloudEnvironmentFound", cloudEnvironment != null)
                        .FluentAddValue("JobCollectedData", JsonConvert.SerializeObject(jobResultData))
                        .FluentAddValue("JobState", jobResultData.JobState);

                    ValidationUtil.IsRequired(jobResultData.EnvironmentId, nameof(jobResultData.EnvironmentId));

                    if (cloudEnvironment == null)
                    {
                        return handlerContext;
                    }

                    if (jobResultData.JobState == JobState.Succeeded)
                    {
                        // Only call resume callback when we are calling back from a resume
                        if (cloudEnvironment.State == CloudEnvironmentState.Starting)
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
                            handlerContext.CloudEnvironment = await environmentManager.ResumeCallbackAsync(
                                cloudEnvironment,
                                Guid.Parse(storageResourceId),
                                string.IsNullOrEmpty(archiveStorageResourceId) ? default(Guid?) : Guid.Parse(archiveStorageResourceId),
                                childLogger.NewChildLogger());
                        }
                    }
                    else
                    {
                        // Mark environment provision to failed status
                        if (cloudEnvironment.State == CloudEnvironmentState.Provisioning)
                        {
                            cloudEnvironment.LastUpdatedByHeartBeat = jobResultData.Timestamp;
                            var newState = CloudEnvironmentState.Failed;
                            var errorMessage = MessageCodeUtils.GetCodeFromError(jobResultData.Errors) ?? MessageCodes.StartEnvironmentGenericError.ToString();
                            handlerContext.CloudEnvironmentState = newState;
                            handlerContext.Reason = errorMessage;
                            handlerContext.Trigger = CloudEnvironmentStateUpdateTriggers.StartEnvironmentJobFailed;
                            return handlerContext;
                        }
                        else if (cloudEnvironment.State == CloudEnvironmentState.Starting)
                        {
                            // Shutdown the environment if the environment has failed to start.
                            var environmentServiceResult = await environmentManager.ForceSuspendAsync(cloudEnvironment, childLogger.NewChildLogger());
                            return new CollectedDataHandlerContext(environmentServiceResult.CloudEnvironment);
                        }
                    }

                    return handlerContext;
                });
        }
    }
}
