// <copyright file="StartEnvironmentResultHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
        private readonly ICloudEnvironmentManager cloudEnvironmentManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartEnvironmentResultHandler"/> class.
        /// </summary>
        /// <param name="environmentManager">Environment Manager.</param>
        public StartEnvironmentResultHandler(ICloudEnvironmentManager environmentManager)
        {
            cloudEnvironmentManager = environmentManager;
        }

        /// <inheritdoc/>
        public bool CanProcess(CollectedData data)
        {
            return data is JobResult && data.Name == JobCommand.StartEnvironment.ToString();
        }

        /// <inheritdoc/>
        public async Task ProcessAsync(CollectedData data, Guid vmResourceId, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
               "start_environment_handler_process",
               async (childLogger) =>
               {
                   if (!CanProcess(data))
                   {
                       throw new InvalidOperationException($"Collected data of type {data?.GetType().Name}, name  {data?.Name} cannot be processed by {nameof(StartEnvironmentResultHandler)}.");
                   }

                   var jobResultData = (JobResult)data;

                   childLogger.FluentAddBaseValue(nameof(CollectedData), JsonConvert.SerializeObject(jobResultData))
                        .FluentAddBaseValue("CloudEnvironmentId", jobResultData.EnvironmentId);

                   if (jobResultData.JobState != JobState.Succeeded)
                   {
                       childLogger.LogError($"Start Environment job failed for virtaul machine : {vmResourceId}");

                       // Mark environment provision to failed status
                       ValidationUtil.IsRequired(jobResultData.EnvironmentId, "Environment Id");

                       var cloudEnvironment = await cloudEnvironmentManager.GetEnvironmentAsync(jobResultData.EnvironmentId, childLogger);
                       if (cloudEnvironment == default)
                       {
                           childLogger.LogInfo($"No environment found for virtual machine id : {vmResourceId} and environment {jobResultData.EnvironmentId}");
                           return;
                       }

                       if (cloudEnvironment.State == CloudEnvironmentState.Provisioning)
                       {
                           cloudEnvironment.LastUpdatedByHeartBeat = jobResultData.Timestamp;
                           var newState = CloudEnvironmentState.Failed;
                           var errorMessage = MessageCodeUtils.GetCodeFromError(jobResultData.Errors) ?? MessageCodes.StartEnvironmentGenericError.ToString();
                           await cloudEnvironmentManager.UpdateEnvironmentAsync(cloudEnvironment, newState, CloudEnvironmentStateUpdateTriggers.StartEnvironmentJobFailed, errorMessage, childLogger);
                           return;
                       }
                       else if (cloudEnvironment.State == CloudEnvironmentState.Starting)
                       {
                           // Shutdown the environment if the environment has failed to start.
                           await this.cloudEnvironmentManager.ForceEnvironmentShutdownAsync(cloudEnvironment, childLogger);
                           return;
                       }
                   }
               });
        }
    }
}
