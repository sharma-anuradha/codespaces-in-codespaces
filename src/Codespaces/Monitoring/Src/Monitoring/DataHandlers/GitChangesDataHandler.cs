// <copyright file="GitChangesDataHandler.cs" company="Microsoft">
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
    /// Handler for <see cref="GitChangesData" />.
    /// </summary>
    public class GitChangesDataHandler : IDataHandler
    {
        private readonly ILatestHeartbeatMonitor latestHeartbeatMonitor;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitChangesDataHandler"/> class.
        /// </summary>
        /// <param name="latestHeartbeatMonitor">Latest Heartbeat Monitor.</param>
        public GitChangesDataHandler(ILatestHeartbeatMonitor latestHeartbeatMonitor)
        {
            this.latestHeartbeatMonitor = latestHeartbeatMonitor;
        }

        /// <inheritdoc />
        public bool CanProcess(CollectedData data)
        {
            return data is GitChangesData;
        }

        /// <inheritdoc />
        public async Task<CollectedDataHandlerContext> ProcessAsync(CollectedData data, CollectedDataHandlerContext handlerContext, Guid vmResourceId, IDiagnosticsLogger logger)
        {
            if (!CanProcess(data))
            {
                throw new InvalidOperationException($"Collected data of type {data?.GetType().Name} cannot be processed by {nameof(GitChangesDataHandler)}.");
            }

            return await logger.OperationScopeAsync(
               "git_changes_data_handler_process",
               (childLogger) =>
               {
                   var gitChangesData = (GitChangesData)data;

                   childLogger.FluentAddBaseValue(nameof(CollectedData), JsonConvert.SerializeObject(gitChangesData));

                   // No-op if the environmentId is empty.
                   if (string.IsNullOrWhiteSpace(gitChangesData.EnvironmentId))
                   {
                       return Task.FromResult(handlerContext);
                   }

                   childLogger.FluentAddBaseValue("CloudEnvironmentId", gitChangesData.EnvironmentId);

                   var cloudEnvironment = handlerContext.CloudEnvironment;
                   ValidateCloudEnvironment(cloudEnvironment, gitChangesData.EnvironmentId);

                   cloudEnvironment.HasUnpushedGitChanges = gitChangesData.HasChanges;

                   return Task.FromResult(handlerContext);
               });
        }

        private void ValidateCloudEnvironment(CloudEnvironment cloudEnvironment, string inputEnvironmentId)
        {
            ValidationUtil.IsTrue(cloudEnvironment != null, $"No environments found matching the EnvironmentId {inputEnvironmentId} from {nameof(EnvironmentSessionData)}");
            ValidationUtil.IsTrue(cloudEnvironment.State != CloudEnvironmentState.Deleted, $"Heartbeat received for a deleted environment {inputEnvironmentId}");
        }
    }
}
