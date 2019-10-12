// <copyright file="EnvironmentDataHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common;

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
        public async Task ProcessAsync(CollectedData data, string vmResourceId, IDiagnosticsLogger logger)
        {
            if (!CanProcess(data))
            {
                throw new Exception($"Collected data of type {data?.GetType().Name} cannot be processed by {nameof(EnvironmentDataHandler)}.");
            }

            var environmentData = (EnvironmentData)data;
            ValidateState(environmentData);

            var cloudEnvironment = await environmentManager.GetEnvironmentByIdAsync(environmentData.EnvironmentId, logger);
            ValidateCloudEnvironment(cloudEnvironment, environmentData.EnvironmentId);

            cloudEnvironment.LastUpdatedByHeartBeat = environmentData.Timestamp;
            cloudEnvironment.Connection.ConnectionSessionPath = environmentData.SessionPath;
            var newState = DetermineNewEnvironmentState(cloudEnvironment, environmentData);
            await environmentManager.UpdateEnvironmentAsync(cloudEnvironment, logger, newState);
        }

        private void ValidateCloudEnvironment(CloudEnvironment cloudEnvironment, string inputEnvironmentId)
        {
            if (cloudEnvironment == null)
            {
                throw new Exception($"No environments found matching the EnvironmentId {inputEnvironmentId} from {nameof(EnvironmentData)}");
            }

            if (cloudEnvironment.State == CloudEnvironmentState.Deleted)
            {
                throw new Exception($"Heartbeat recieved for a deleted environment {inputEnvironmentId}");
            }
        }

        private void ValidateState(EnvironmentData environmentData)
        {
            if (string.IsNullOrEmpty(environmentData.EnvironmentId))
            {
                throw new Exception($"Environment Id is empty for {nameof(EnvironmentData)}");
            }
        }

        private CloudEnvironmentState DetermineNewEnvironmentState(CloudEnvironment cloudEnvironment, EnvironmentData environmentData)
        {
            switch (environmentData.EnvironmentType)
            {
                case VsoEnvironmentType.ContainerBased:
                    return DetermineNewStateForDockerBasedEnvironment(cloudEnvironment, environmentData);

                case VsoEnvironmentType.VirtualMachineBased:
                    return DetermineNewStateForVmBasedEnvironment(cloudEnvironment, environmentData);

                default:
                    return default;
            }
        }

        private CloudEnvironmentState DetermineNewStateForDockerBasedEnvironment(CloudEnvironment cloudEnvironment, EnvironmentData environmentData)
        {
            // Check if all the required flags are set
            var isEnvRunning = (environmentData.State & (VsoEnvironmentState.DockerDaemonRunning |
                                                        VsoEnvironmentState.ContainerRunning |
                                                        VsoEnvironmentState.CliBootstrapRunning |
                                                        VsoEnvironmentState.VslsAgentRunning |
                                                        VsoEnvironmentState.VslsRelayConnected))
                                                        ==
                                                        (VsoEnvironmentState.DockerDaemonRunning |
                                                        VsoEnvironmentState.ContainerRunning |
                                                        VsoEnvironmentState.CliBootstrapRunning |
                                                        VsoEnvironmentState.VslsAgentRunning |
                                                        VsoEnvironmentState.VslsRelayConnected);

            if (isEnvRunning)
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
            return default;
        }
    }
}
