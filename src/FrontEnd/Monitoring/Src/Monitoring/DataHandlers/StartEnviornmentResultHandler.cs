// <copyright file="StartEnviornmentResultHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring.DataHandlers
{
    /// <summary>
    /// 
    /// </summary>
    public class StartEnviornmentResultHandler : IDataHandler
    {
        private readonly ICloudEnvironmentManager cloudEnvironmentManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartEnviornmentResultHandler"/> class.
        /// </summary>
        /// <param name="environmentManager">Environment Manager.</param>
        public StartEnviornmentResultHandler(ICloudEnvironmentManager environmentManager)
        {
            this.cloudEnvironmentManager = environmentManager;
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
                       throw new InvalidOperationException($"Collected data of type {data?.GetType().Name}, name  {data?.Name} cannot be processed by {nameof(StartEnviornmentResultHandler)}.");
                   }

                   childLogger.FluentAddBaseValue(nameof(data.Name), data.Name)
                         .FluentAddBaseValue("JobResult", JsonConvert.SerializeObject(data));

                   var jobResultData = (JobResult)data;
                   if (jobResultData.JobState != JobState.Succeeded)
                   {
                       childLogger.LogError($"Start Environment job failed for virtaul machine : {vmResourceId}");

                       // Mark environment provision to failed status
                       if (string.IsNullOrEmpty(jobResultData.EnvironmentId))
                       {
                           throw new ArgumentException($"Environment id is null or empty for Start environment job result from virtual machine, {vmResourceId}");
                       }

                       var cloudEnvironment = await cloudEnvironmentManager.GetEnvironmentByIdAsync(jobResultData.EnvironmentId, childLogger);
                       if (cloudEnvironment == default)
                       {
                           childLogger.LogInfo($"No environment found for virtual machine id : {vmResourceId} and environment {jobResultData.EnvironmentId}");
                           return;
                       }

                       if (cloudEnvironment.State == CloudEnvironmentState.Provisioning)
                       {
                           cloudEnvironment.LastUpdatedByHeartBeat = jobResultData.Timestamp;
                           var newState = CloudEnvironmentState.Failed;
                           await cloudEnvironmentManager.UpdateEnvironmentAsync(cloudEnvironment, childLogger, newState);
                           return;
                       }
                       else if (cloudEnvironment.State == CloudEnvironmentState.Starting)
                       {
                           // Shutdown the environment if the environment has failed to start.
                           await this.cloudEnvironmentManager.ShutdownEnvironmentAsync(cloudEnvironment.Id, vmResourceId.ToString(), childLogger);
                           return;
                       }
                   }
               });
        }
    }
}
