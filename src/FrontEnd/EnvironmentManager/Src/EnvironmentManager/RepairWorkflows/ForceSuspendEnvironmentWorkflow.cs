// <copyright file="ForceSuspendEnvironmentWorkflow.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows
{
    /// <summary>
    /// Force suspend environment.
    /// </summary>
    public class ForceSuspendEnvironmentWorkflow : IForceSuspendEnvironmentWorkflow
    {
        private const string LogBaseName = "force_suspend_environment_repair";

        /// <summary>
        /// Initializes a new instance of the <see cref="ForceSuspendEnvironmentWorkflow"/> class.
        /// </summary>
        /// <param name="environmentStateManager">target environment state manager.</param>
        /// <param name="resourceBrokerHttpClient">target resource Broker Http Client.</param>
        /// <param name="environmentRepository">target repository.</param>
        public ForceSuspendEnvironmentWorkflow(
            IEnvironmentStateManager environmentStateManager,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            ICloudEnvironmentRepository environmentRepository)
        {
            EnvironmentStateManager = environmentStateManager;
            ResourceBrokerHttpClient = resourceBrokerHttpClient;
            EnvironmentRepository = environmentRepository;
        }

        /// <inheritdoc/>
        public EnvironmentRepairActions WorkflowType => EnvironmentRepairActions.ForceSuspend;

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerHttpClient { get; }

        private ICloudEnvironmentRepository EnvironmentRepository { get; }

        /// <inheritdoc/>
        public async Task ExecuteAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(logger, nameof(logger));

            await logger.OperationScopeAsync(
                $"{LogBaseName}_execute",
                async (childLogger) =>
                {
                    // Deal with getting the state to the correct place
                    var shutdownState = CloudEnvironmentState.Shutdown;
                    if (cloudEnvironment?.Storage?.Type == ResourceType.StorageArchive)
                    {
                        shutdownState = CloudEnvironmentState.Archived;
                    }

                    // Set the state of the environement
                    await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, shutdownState, CloudEnvironmentStateUpdateTriggers.ForceEnvironmentShutdown, null, logger);

                    var computeIdToken = cloudEnvironment.Compute?.ResourceId;
                    cloudEnvironment.Compute = null;

                    // Update the database state.
                    cloudEnvironment = await EnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());

                    // Delete the allocated resources.
                    if (computeIdToken != null)
                    {
                        await ResourceBrokerHttpClient.DeleteAsync(
                            Guid.Parse(cloudEnvironment.Id),
                            computeIdToken.Value,
                            childLogger.NewChildLogger());
                    }
                },
                swallowException: true);
        }
    }
}
