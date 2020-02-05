// <copyright file="ForceSuspendEnvironmentWorkflow.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers
{
    /// <summary>
    /// Force suspend environment.
    /// </summary>
    public class ForceSuspendEnvironmentWorkflow : IForceSuspendEnvironmentWorkflow
    {
        private const string LogBaseName = "suspend_environment_workflow";

        /// <summary>
        /// Initializes a new instance of the <see cref="ForceSuspendEnvironmentWorkflow"/> class.
        /// </summary>
        /// <param name="environmentStateManager">target environment state manager.</param>
        /// <param name="resourceBrokerHttpClient">target resource Broker Http Client.</param>
        /// <param name="environmentRepository">target repository.</param>
        public ForceSuspendEnvironmentWorkflow(
            IEnvironmentStateManager environmentStateManager,
            IResourceBrokerResourcesHttpContract resourceBrokerHttpClient,
            ICloudEnvironmentRepository environmentRepository)
        {
            EnvironmentStateManager = environmentStateManager;
            ResourceBrokerHttpClient = resourceBrokerHttpClient;
            EnvironmentRepository = environmentRepository;
        }

        /// <inheritdoc/>
        public EnvironmentRepairActions WorkflowType => EnvironmentRepairActions.ForceSuspend;

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private IResourceBrokerResourcesHttpContract ResourceBrokerHttpClient { get; }

        private ICloudEnvironmentRepository EnvironmentRepository { get; }

        /// <inheritdoc/>
        public async Task ExecuteAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));
            Requires.NotNull(logger, nameof(logger));

            await logger.OperationScopeAsync(
                $"{LogBaseName}_force_suspend",
                async (childLogger) =>
                {
                    await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Shutdown, CloudEnvironmentStateUpdateTriggers.ForceEnvironmentShutdown, null, logger);

                    var computeIdToken = cloudEnvironment.Compute?.ResourceId;
                    cloudEnvironment.Compute = null;

                    // Update the database state.
                    await EnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());

                    // Delete the allocated resources.
                    if (computeIdToken != null)
                    {
                        await ResourceBrokerHttpClient.DeleteAsync(computeIdToken.Value, childLogger.NewChildLogger());
                    }
                },
                swallowException: true);
        }
    }
}
