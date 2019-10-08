// <copyright file="LinuxDockerStateHandler.cs" company="Microsoft">
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
    /// Handler for LinuxDockerState.
    /// </summary>
    public class LinuxDockerStateHandler : IDataHandler
    {
        private readonly ICloudEnvironmentManager environmentManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxDockerStateHandler"/> class.
        /// </summary>
        /// <param name="environmentManager">Environment Manager.</param>
        /// <param name="currentUserProvider">Current User Provider.</param>
        public LinuxDockerStateHandler(ICloudEnvironmentManager environmentManager)
        {
            this.environmentManager = environmentManager;
        }

        /// <inheritdoc />
        public bool CanProcess(AbstractMonitorState state)
        {
            return state is LinuxDockerState;
        }

        /// <inheritdoc />
        public async Task ProcessAsync(AbstractMonitorState state, string vmResourceId, IDiagnosticsLogger logger)
        {
            if (!CanProcess(state))
            {
                throw new Exception($"States of type {state.GetType().Name} cannot be processed by {nameof(LinuxDockerStateHandler)}.");
            }

            var linuxDockerState = (LinuxDockerState)state;
            ValidateState(linuxDockerState);

            var cloudEnvironment = await environmentManager.GetEnvironmentByIdAsync(linuxDockerState.EnvironmentId, logger);
            ValidateCloudEnvironment(cloudEnvironment, linuxDockerState.EnvironmentId);

            cloudEnvironment.LastUpdatedByHeartBeat = linuxDockerState.TimeStamp;
            cloudEnvironment.Connection.ConnectionSessionPath = linuxDockerState.SessionPath;
            var newState = DetermineNewEnvironmentState(cloudEnvironment, linuxDockerState, logger);
            await environmentManager.UpdateEnvironmentAsync(cloudEnvironment, logger, newState);
        }

        private void ValidateCloudEnvironment(CloudEnvironment cloudEnvironment, string inputEnvironmentId)
        {
            if (cloudEnvironment == null)
            {
                throw new Exception($"No environments found matching the EnvironmentId {inputEnvironmentId} from {nameof(LinuxDockerState)}");
            }

            if (cloudEnvironment.State == CloudEnvironmentState.Deleted)
            {
                throw new Exception($"Monitoring update recieved for a deleted environment {inputEnvironmentId}");
            }
        }

        private void ValidateState(LinuxDockerState linuxDockerState)
        {
            if (linuxDockerState.EnvironmentId == null)
            {
                throw new Exception($"Environment Id is null for {nameof(LinuxDockerState)}");
            }
        }

        private CloudEnvironmentState DetermineNewEnvironmentState(CloudEnvironment cloudEnvironment, LinuxDockerState linuxDockerState, IDiagnosticsLogger logger)
        {
            // Check if all the required flags are set
            var isEnvRunning = (linuxDockerState.State & (EnvironmentRunningState.DockerDaemonRunning |
                                                        EnvironmentRunningState.ContainerRunning |
                                                        EnvironmentRunningState.CliBootstrapRunning |
                                                        EnvironmentRunningState.VslsAgentRunning))
                                                        ==
                                                        (EnvironmentRunningState.DockerDaemonRunning |
                                                        EnvironmentRunningState.ContainerRunning |
                                                        EnvironmentRunningState.CliBootstrapRunning |
                                                        EnvironmentRunningState.VslsAgentRunning);

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
    }
}
