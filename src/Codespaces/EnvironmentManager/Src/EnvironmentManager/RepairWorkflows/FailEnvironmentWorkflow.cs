// <copyright file="FailEnvironmentWorkflow.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows
{
    /// <summary>
    /// Fails an environment.
    /// </summary>
    public class FailEnvironmentWorkflow : IFailEnvironmentWorkflow
    {
        private const string LogBaseName = "fail_environment_repair";

        /// <summary>
        /// Initializes a new instance of the <see cref="FailEnvironmentWorkflow"/> class.
        /// </summary>
        /// <param name="environmentStateManager">target environment state manager.</param>
        /// <param name="resourceBrokerHttpClient">target resource Broker Http Client.</param>
        /// <param name="environmentRepository">target repository.</param>
        public FailEnvironmentWorkflow(
            IEnvironmentStateManager environmentStateManager,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            ICloudEnvironmentRepository environmentRepository)
        {
            EnvironmentStateManager = environmentStateManager;
            ResourceBrokerClient = resourceBrokerHttpClient;
            EnvironmentRepository = environmentRepository;
        }

        /// <inheritdoc/>
        public EnvironmentRepairActions WorkflowType => EnvironmentRepairActions.Fail;

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

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
                    // Update state to be failed
                    await EnvironmentStateManager.SetEnvironmentStateAsync(
                        cloudEnvironment,
                        CloudEnvironmentState.Failed,
                        nameof(FailEnvironmentWorkflow),
                        string.Empty,
                        null,
                        childLogger);

                    var cloudEnvironmentId = Guid.Parse(cloudEnvironment.Id);

                    // Delete the allocated resources.
                    if (cloudEnvironment.Compute != null)
                    {
                        await ResourceBrokerClient.DeleteAsync(cloudEnvironmentId, cloudEnvironment.Compute.ResourceId, childLogger.NewChildLogger());
                    }

                    if (cloudEnvironment.OSDisk != null)
                    {
                        await ResourceBrokerClient.DeleteAsync(cloudEnvironmentId, cloudEnvironment.OSDisk.ResourceId, childLogger.NewChildLogger());
                    }

                    if (cloudEnvironment.Storage != null)
                    {
                        await ResourceBrokerClient.DeleteAsync(cloudEnvironmentId, cloudEnvironment.Storage.ResourceId, childLogger.NewChildLogger());
                    }

                    // Update the database state
                    await EnvironmentRepository.UpdateAsync(cloudEnvironment, childLogger);
                },
                swallowException: true);
        }
    }
}
