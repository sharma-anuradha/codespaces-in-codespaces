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
        private readonly ILatestHeartbeatMonitor latestHeartbeatMonitor;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSessionDataHandler"/> class.
        /// </summary>
        /// <param name="latestHeartbeatMonitor">Latest Heartbeat Monitor.</param>
        public EnvironmentSessionDataHandler(
            ILatestHeartbeatMonitor latestHeartbeatMonitor)
        {
            this.latestHeartbeatMonitor = latestHeartbeatMonitor;
        }

        /// <inheritdoc />
        public bool CanProcess(CollectedData data)
        {
            return data is EnvironmentSessionData;
        }

        /// <inheritdoc />
        public async Task<CollectedDataHandlerContext> ProcessAsync(CollectedData data, CollectedDataHandlerContext handlerContext, Guid vmResourceId, IDiagnosticsLogger logger)
        {
            if (!CanProcess(data))
            {
                throw new InvalidOperationException($"Collected data of type {data?.GetType().Name} cannot be processed by {nameof(EnvironmentSessionDataHandler)}.");
            }

            return await logger.OperationScopeAsync(
               "environment_session_data_handler_process",
               (childLogger) =>
               {
                   var environmentSessionData = (EnvironmentSessionData)data;

                   childLogger.FluentAddBaseValue(nameof(CollectedData), JsonConvert.SerializeObject(environmentSessionData));

                   // No-op if the environmentId is empty.
                   if (string.IsNullOrWhiteSpace(environmentSessionData.EnvironmentId))
                   {
                       return Task.FromResult(handlerContext);
                   }

                   childLogger.FluentAddBaseValue("CloudEnvironmentId", environmentSessionData.EnvironmentId);

                   var cloudEnvironment = handlerContext.CloudEnvironment;
                   ValidateCloudEnvironment(cloudEnvironment, environmentSessionData.EnvironmentId);

                   cloudEnvironment.LastUpdatedByHeartBeat = environmentSessionData.Timestamp;
                   latestHeartbeatMonitor.UpdateHeartbeat(environmentSessionData.Timestamp);

                   if (environmentSessionData.ConnectedSessionCount > 0 && (cloudEnvironment.LastUsed == null || cloudEnvironment.LastUsed < environmentSessionData.Timestamp))
                   {
                       cloudEnvironment.LastUsed = environmentSessionData.Timestamp;
                   }

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
