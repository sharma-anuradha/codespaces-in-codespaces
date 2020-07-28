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
        private readonly IEnvironmentManager environmentManager;
        private readonly IEnvironmentMonitor environmentMonitor;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentDataHandler"/> class.
        /// </summary>
        /// <param name="environmentManager">Environment Manager.</param>
        /// <param name="environmentMonitor">Environment Monitor.</param>
        public EnvironmentDataHandler(
            IEnvironmentManager environmentManager,
            IEnvironmentMonitor environmentMonitor)
        {
            this.environmentManager = environmentManager;
            this.environmentMonitor = environmentMonitor;
        }

        /// <inheritdoc />
        public bool CanProcess(CollectedData data)
        {
            return data is EnvironmentData;
        }

        /// <inheritdoc />
        public async Task<CollectedDataHandlerContext> ProcessAsync(CollectedData data, CollectedDataHandlerContext handlerContext, Guid vmResourceId, IDiagnosticsLogger logger)
        {
            if (!CanProcess(data))
            {
                throw new InvalidOperationException($"Collected data of type {data?.GetType().Name} cannot be processed by {nameof(EnvironmentDataHandler)}.");
            }

            return await logger.OperationScopeAsync(
               "environment_data_handler_process",
               async (childLogger) =>
               {
                   var environmentData = (EnvironmentData)data;

                   childLogger.FluentAddBaseValue(nameof(CollectedData), JsonConvert.SerializeObject(environmentData));

                   // No-op if the environmentId is empty.
                   if (string.IsNullOrEmpty(environmentData.EnvironmentId))
                   {
                       return handlerContext;
                   }

                   childLogger.FluentAddBaseValue("CloudEnvironmentId", environmentData.EnvironmentId);

                   var environment = handlerContext.CloudEnvironment;
                   ValidateEnvironment(environment, environmentData.EnvironmentId);

                   environment.LastUpdatedByHeartBeat = environmentData.Timestamp;

                   // This switch gives preference to the existing value instead of the incomming value.
                   // This prevents new values from ovewritting the existing one.
                   environment.Connection.ConnectionSessionPath = !string.IsNullOrWhiteSpace(environment.Connection.ConnectionSessionPath) ? environment.Connection.ConnectionSessionPath : environmentData.SessionPath;
                   var newState = DetermineNewEnvironmentState(environment, environmentData);
                   handlerContext.CloudEnvironmentState = newState.state;
                   handlerContext.Reason = newState.reason.ToString();

                   if (environment.Type == EnvironmentType.CloudEnvironment)
                   {
                       // Shutdown if the environment is idle
                       if (environmentData.State.HasFlag(VsoEnvironmentState.Idle))
                       {
                           environment = await environmentManager.SuspendAsync(Guid.Parse(environment.Id), childLogger);
                           environment.LastUpdatedByHeartBeat = environmentData.Timestamp;
                           return new CollectedDataHandlerContext(environment);
                       }
                       else if (newState.state == CloudEnvironmentState.Unavailable)
                       {
                           // Check that environment state has transitioned back to avaiable within defined timeout, if not force suspend the environment.
                           await environmentMonitor.MonitorUnavailableStateTransitionAsync(environment.Id, environment.Compute.ResourceId, childLogger.NewChildLogger());
                       }
                   }

                   return handlerContext;
               });
        }

        private void ValidateEnvironment(CloudEnvironment environment, string inputEnvironmentId)
        {
            ValidationUtil.IsTrue(environment != null, $"No environments found matching the EnvironmentId {inputEnvironmentId} from {nameof(EnvironmentData)}");
            ValidationUtil.IsTrue(environment.State != CloudEnvironmentState.Deleted, $"Heartbeat received for a deleted environment {inputEnvironmentId}");
        }

        private (CloudEnvironmentState state, int? reason) DetermineNewEnvironmentState(CloudEnvironment environment, EnvironmentData environmentData)
        {
            var environmentIsRunning = environment.Type == EnvironmentType.CloudEnvironment ?
                IsCloudEnvironmentRunning(environmentData) :
                IsSelfHostedEnvironmentRunning(environmentData);

            if (environmentIsRunning)
            {
                if (CanChangeStateToAvailable(environment))
                {
                    return (CloudEnvironmentState.Available, null);
                }
            }
            else
            {
                // If current state is Available, change status to Unavailable (when Enviroment is NOT fully running).
                if (environment.State == CloudEnvironmentState.Available)
                {
                    return (CloudEnvironmentState.Unavailable, (int)MessageCodes.HeartbeatUnhealthy);
                }
            }

            return default;
        }

        private bool CanChangeStateToAvailable(CloudEnvironment environment)
        {
            return environment.State != CloudEnvironmentState.Archived &&
                   environment.State != CloudEnvironmentState.Available &&
                   environment.State != CloudEnvironmentState.Deleted &&
                   environment.State != CloudEnvironmentState.Shutdown &&
                   environment.State != CloudEnvironmentState.ShuttingDown;
        }

        private bool IsCloudEnvironmentRunning(EnvironmentData environmentData)
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

        private bool IsSelfHostedEnvironmentRunning(EnvironmentData environmentData)
        {
            var state = environmentData.State;
            var runningState = VsoEnvironmentState.VslsAgentRunning;

            return state.HasFlag(runningState);
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
            return environmentData.State == VsoEnvironmentState.VslsRelayConnected;
        }
    }
}
