// <copyright file="InactiveEnvironmentWorkflow.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows
{
    /// <summary>
    /// Used to mark an environment as inactive.
    /// </summary>
    public class InactiveEnvironmentWorkflow : IInactiveEnvironmentWorkflow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InactiveEnvironmentWorkflow"/> class.
        /// </summary>
        /// <param name="environmentStateManager">target environment state manager.</param>
        /// <param name="environmentRepository">The environment repository for updating the actual state in the db.</param>
        public InactiveEnvironmentWorkflow(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository environmentRepository)
        {
            EnvironmentStateManager = environmentStateManager;
            EnvironmentRepository = environmentRepository;
        }

        /// <inheritdoc />
        public EnvironmentRepairActions WorkflowType => EnvironmentRepairActions.Unavailable;

        /// <summary>
        /// Gets the Environment State manager for setting the appropriate state.
        /// </summary>
        private IEnvironmentStateManager EnvironmentStateManager { get; }

        /// <summary>
        /// Gets the Environment repository for updating the environment state changes.
        /// </summary>
        private ICloudEnvironmentRepository EnvironmentRepository { get; }

        /// <inheritdoc />
        public async Task ExecuteAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Unavailable, CloudEnvironmentStateUpdateTriggers.EnvironmentMonitor, "EnvironmentUnavailable", logger);
            await EnvironmentRepository.UpdateAsync(cloudEnvironment, logger);
        }
    }
}
