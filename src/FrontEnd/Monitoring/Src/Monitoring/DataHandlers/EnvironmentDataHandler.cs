// <copyright file="EnvironmentDataHandler.cs" company="Microsoft">
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
    /// Handler for <see cref="EnvironmentData" />.
    /// </summary>
    public class EnvironmentDataHandler : IDataHandler
    {
        private readonly ICloudEnvironmentManager environmentManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentDataHandler"/> class.
        /// </summary>
        /// <param name="environmentManager">Environment Manager.</param>
        public EnvironmentDataHandler(ICloudEnvironmentManager environmentManager)
        {
            this.environmentManager = environmentManager;
        }

        /// <inheritdoc />
        public bool CanProcess(CollectedData data)
        {
            return data is EnvironmentData;
        }

        /// <inheritdoc />
        public async Task ProcessAsync(CollectedData data, Guid vmResourceId, IDiagnosticsLogger logger)
        {
            if (!CanProcess(data))
            {
                throw new Exception($"Collected data of type {data?.GetType().Name} cannot be processed by {nameof(EnvironmentDataHandler)}.");
            }

            await logger.OperationScopeAsync(
               "environment_data_handler_process",
               async (childLogger) =>
               {
                   var environmentData = (EnvironmentData)data;

                   childLogger.FluentAddBaseValue(nameof(CollectedData), JsonConvert.SerializeObject(environmentData));

                   // No-op if the environmentId is empty.
                   if (string.IsNullOrEmpty(environmentData.EnvironmentId))
                   {
                       return;
                   }

                   childLogger.FluentAddBaseValue("CloudEnvironmentId", environmentData.EnvironmentId);

                   var cloudEnvironment = await environmentManager.GetEnvironmentByIdAsync(environmentData.EnvironmentId, childLogger);
                   ValidateCloudEnvironment(cloudEnvironment, environmentData.EnvironmentId);

                   cloudEnvironment.LastUpdatedByHeartBeat = environmentData.Timestamp;
                   cloudEnvironment.Connection.ConnectionSessionPath = environmentData.SessionPath;
                   var newState = DetermineNewEnvironmentState(cloudEnvironment, environmentData);
                   await environmentManager.UpdateEnvironmentAsync(cloudEnvironment, newState, CloudEnvironmentStateUpdateReasons.Heartbeat, childLogger);

                   // Shutdown if the environment is idle
                   if (environmentData.State.HasFlag(VsoEnvironmentState.Idle))
                   {
                       await environmentManager.ShutdownEnvironmentAsync(cloudEnvironment.Id, vmResourceId.ToString(), childLogger);
                   }
               });
        }

        private void ValidateCloudEnvironment(CloudEnvironment cloudEnvironment, string inputEnvironmentId)
        {
            ValidationUtil.IsTrue(cloudEnvironment != null, $"No environments found matching the EnvironmentId {inputEnvironmentId} from {nameof(EnvironmentData)}");
            ValidationUtil.IsTrue(cloudEnvironment.State != CloudEnvironmentState.Deleted, $"Heartbeat received for a deleted environment {inputEnvironmentId}");
        }

        private CloudEnvironmentState DetermineNewEnvironmentState(CloudEnvironment cloudEnvironment, EnvironmentData environmentData)
        {
            switch (environmentData.EnvironmentType)
            {
                case VsoEnvironmentType.ContainerBased:
                    return DetermineNewStateForContainerBasedEnvironment(cloudEnvironment, environmentData);

                case VsoEnvironmentType.VirtualMachineBased:
                    return DetermineNewStateForVmBasedEnvironment(cloudEnvironment, environmentData);

                default:
                    return default;
            }
        }

        private CloudEnvironmentState DetermineNewStateForContainerBasedEnvironment(CloudEnvironment cloudEnvironment, EnvironmentData environmentData)
        {
            if (IsEnvironmentRunning(environmentData))
            {
                // If current state is NOT Available, change status to Available (when Enviroment is fully running).
                if (cloudEnvironment.State != CloudEnvironmentState.Available)
                {
                    return CloudEnvironmentState.Available;
                }
            }
            else
            {
                // If current state is Available, change status to Unavailable (when Enviroment is NOT fully running).
                if (cloudEnvironment.State == CloudEnvironmentState.Available)
                {
                    return CloudEnvironmentState.Unavailable;
                }
            }

            return default;
        }

        private CloudEnvironmentState DetermineNewStateForVmBasedEnvironment(CloudEnvironment cloudEnvironment, EnvironmentData environmentData)
        {
            throw new NotImplementedException();
        }

        private bool IsEnvironmentRunning(EnvironmentData environmentData)
        {
            switch (environmentData.EnvironmentType)
            {
                case VsoEnvironmentType.ContainerBased:
                    return IsContainerBasedEnvironmentRunning(environmentData);

                case VsoEnvironmentType.VirtualMachineBased:
                    return IsVmBasedEnvironmentRunning(environmentData);

                default:
                    return default;
            }
        }

        private bool IsContainerBasedEnvironmentRunning(EnvironmentData environmentData)
        {
            var state = environmentData.State;
            var runningState = VsoEnvironmentState.DockerDaemonRunning
                               | VsoEnvironmentState.ContainerRunning
                               | VsoEnvironmentState.CliBootstrapRunning
                               | VsoEnvironmentState.VslsAgentRunning
                               | VsoEnvironmentState.VslsRelayConnected;

            return state.HasFlag(runningState);
        }

        private bool IsVmBasedEnvironmentRunning(EnvironmentData environmentData)
        {
            throw new NotImplementedException();
        }
    }
}
