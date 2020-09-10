// <copyright file="ExportEnvironmentResultHandler.cs" company="Microsoft">
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
    /// Process export environment job result.
    /// </summary>
    public class ExportEnvironmentResultHandler : IDataHandler
    {
        private readonly IEnvironmentManager environmentManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportEnvironmentResultHandler"/> class.
        /// </summary>
        /// <param name="environmentManager">Environment Manager.</param>
        public ExportEnvironmentResultHandler(
            IEnvironmentManager environmentManager)
        {
            this.environmentManager = environmentManager;
        }

        /// <inheritdoc/>
        public bool CanProcess(CollectedData data)
        {
            return data is JobResult && (data.Name == JobCommand.ExportEnvironment.ToString());
        }

        /// <inheritdoc/>
        public Task<CollectedDataHandlerContext> ProcessAsync(CollectedData data, CollectedDataHandlerContext handlerContext, Guid vmResourceId, IDiagnosticsLogger logger)
        {
            // TODO t-aibha: refactor this class with StartEnvironmentResultHandler once Ed merges his changes for exporting with git
            return logger.OperationScopeAsync(
                "export_environment_handler_process",
                async (childLogger) =>
                {
                    if (!CanProcess(data))
                    {
                        throw new InvalidOperationException($"Collected data of type {data?.GetType().Name}, name  {data?.Name} cannot be processed by {nameof(ExportEnvironmentResultHandler)}.");
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
                        if (cloudEnvironment.State == CloudEnvironmentState.Exporting)
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

                            var exportSasToken = payloadStageResult.Data.GetValueOrDefault("storageExportReadAccountSasToken");
                            var branchName = payloadStageResult.Data.GetValueOrDefault("BRANCH_NAME");

                            logger.FluentAddBaseValue("ComputeResourceId", computeResourceId)
                                .FluentAddBaseValue("StorageResourceId", storageResourceId)
                                .FluentAddBaseValue("ArchiveStorageResourceId", archiveStorageResourceId);

                            if (Guid.TryParse(storageResourceId, out var storageResourceIdGuid))
                            {
                                // Call export callback async
                                cloudEnvironment = await environmentManager.ExportCallbackAsync(
                                    Guid.Parse(cloudEnvironment.Id),
                                    storageResourceIdGuid,
                                    string.IsNullOrEmpty(archiveStorageResourceId) ? default(Guid?) : Guid.Parse(archiveStorageResourceId),
                                    exportSasToken,
                                    branchName,
                                    childLogger.NewChildLogger());

                                // Call suspend async to shut down environment after exporting is done.
                                handlerContext.CloudEnvironment = await environmentManager.SuspendAsync(Guid.Parse(cloudEnvironment.Id), childLogger.NewChildLogger());
                            }

                            // Set data of handler context to update state
                            handlerContext.CloudEnvironmentState = handlerContext.CloudEnvironment.State;

                            return handlerContext;
                        }
                    }
                    else if (jobResultData.JobState == JobState.Failed)
                    {
                        if (cloudEnvironment.State == CloudEnvironmentState.Exporting)
                        {
                            // Shutdown the environment if the environment has failed to start.
                            handlerContext.CloudEnvironment = await environmentManager.SuspendAsync(Guid.Parse(cloudEnvironment.Id), childLogger.NewChildLogger());

                            // Track failure
                            var errorMessage = MessageCodeUtils.GetCodeFromError(jobResultData.Errors) ?? MessageCodes.ExportEnvironmentGenericError.ToString();
                            handlerContext.Reason = errorMessage;
                            handlerContext.Trigger = CloudEnvironmentStateUpdateTriggers.ExportEnvironmentJobFailed;

                            return handlerContext;
                        }
                    }
                    else if (jobResultData.JobState == JobState.Started)
                    {
                        // begin transition monitoring if a timeout was specified.
                        if (jobResultData.Timeout.HasValue)
                        {
                            // update the environment's state timeout to the one provided by the agent.
                            cloudEnvironment.StateTimeout = jobResultData.Timeout;
                        }
                    }

                    return handlerContext;
                });
        }
    }
}
