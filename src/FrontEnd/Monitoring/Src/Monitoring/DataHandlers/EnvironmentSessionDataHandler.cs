// <copyright file="EnvironmentSessionDataHandler.cs" company="Microsoft">
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
    /// Handler for <see cref="EnvironmentSessionData" />.
    /// </summary>
    public class EnvironmentSessionDataHandler : IDataHandler
    {
        private readonly IEnvironmentManager environmentManager;
        private readonly ILatestHeartbeatMonitor latestHeartbeatMonitor;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSessionDataHandler"/> class.
        /// </summary>
        /// <param name="environmentManager">Environment Manager.</param>
        /// <param name="latestHeartbeatMonitor">Latest Heartbeat Monitor.</param>
        public EnvironmentSessionDataHandler(
            IEnvironmentManager environmentManager,
            ILatestHeartbeatMonitor latestHeartbeatMonitor)
        {
            this.environmentManager = environmentManager;
            this.latestHeartbeatMonitor = latestHeartbeatMonitor;
        }

        /// <inheritdoc />
        public bool CanProcess(CollectedData data)
        {
            return data is EnvironmentSessionData;
        }

        /// <inheritdoc />
        public async Task ProcessAsync(CollectedData data, Guid vmResourceId, IDiagnosticsLogger logger)
        {
            if (!CanProcess(data))
            {
                throw new InvalidOperationException($"Collected data of type {data?.GetType().Name} cannot be processed by {nameof(EnvironmentSessionDataHandler)}.");
            }

            await logger.OperationScopeAsync(
               "environment_session_data_handler_process",
               async (childLogger) =>
               {
                   var environmentSessionData = (EnvironmentSessionData)data;

                   childLogger.FluentAddBaseValue(nameof(CollectedData), JsonConvert.SerializeObject(environmentSessionData));

                   // No-op if the environmentId is empty.
                   if (string.IsNullOrWhiteSpace(environmentSessionData.EnvironmentId))
                   {
                       return;
                   }

                   childLogger.FluentAddBaseValue("CloudEnvironmentId", environmentSessionData.EnvironmentId);

                   var cloudEnvironment = await environmentManager.GetAsync(environmentSessionData.EnvironmentId, childLogger);
                   ValidateCloudEnvironment(cloudEnvironment, environmentSessionData.EnvironmentId);

                   cloudEnvironment.LastUpdatedByHeartBeat = environmentSessionData.Timestamp;
                   latestHeartbeatMonitor.UpdateHeartbeat(environmentSessionData.Timestamp);

                   if (environmentSessionData.ConnectedSessionCount > 0 && cloudEnvironment.LastUsed < environmentSessionData.Timestamp)
                   {
                       cloudEnvironment.LastUsed = environmentSessionData.Timestamp;
                   }

                   cloudEnvironment = await environmentManager.UpdateAsync(cloudEnvironment, CloudEnvironmentState.None, CloudEnvironmentStateUpdateTriggers.Heartbeat, string.Empty, childLogger);
               });
        }

        private void ValidateCloudEnvironment(CloudEnvironment cloudEnvironment, string inputEnvironmentId)
        {
            ValidationUtil.IsTrue(cloudEnvironment != null, $"No environments found matching the EnvironmentId {inputEnvironmentId} from {nameof(EnvironmentSessionData)}");
            ValidationUtil.IsTrue(cloudEnvironment.State != CloudEnvironmentState.Deleted, $"Heartbeat received for a deleted environment {inputEnvironmentId}");
        }
    }
}
