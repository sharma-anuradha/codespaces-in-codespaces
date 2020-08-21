// <copyright file="EnvironmentIntializeResumeAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Intialize Resume Action.
    /// </summary>
    public class EnvironmentIntializeResumeAction : EnvironmentBaseIntializeStartAction<EnvironmentResumeActionInput>, IEnvironmentIntializeResumeAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentIntializeResumeAction"/> class.
        /// </summary>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentAccessManager">Target environment access manager.</param>
        /// <param name="environmentResumeAction">Target environment resume action.</param>
        /// <param name="skuCatalog">Target sku catalog.</param>
        /// <param name="skuUtils">Target skuUtils, to find sku's eligiblity.</param>
        public EnvironmentIntializeResumeAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            IEnvironmentResumeAction environmentResumeAction,
            ISkuCatalog skuCatalog,
            ISkuUtils skuUtils)
            : base(
                  environmentStateManager,
                  repository,
                  currentLocationProvider,
                  currentUserProvider,
                  controlPlaneInfo,
                  environmentAccessManager,
                  skuCatalog,
                  skuUtils)
        {
            EnvironmentResumeAction = environmentResumeAction;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_intialize_resume_action";

        private IEnvironmentResumeAction EnvironmentResumeAction { get; }

        /// <inheritdoc/>
        public Task<CloudEnvironment> RunAsync(Guid environmentId, StartCloudEnvironmentParameters startEnvironmentParams, IDiagnosticsLogger logger)
        {
            // Base Validation
            Requires.NotEmpty(environmentId, nameof(environmentId));
            logger.AddEnvironmentId(environmentId.ToString());

            // Build input
            var input = new EnvironmentResumeActionInput(environmentId)
            {
                StartEnvironmentParams = startEnvironmentParams,
            };

            return RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(
            EnvironmentResumeActionInput input,
            object transientState,
            IDiagnosticsLogger logger)
        {
            var record = await FetchAsync(input, logger);

            var canProceed = ConfigureRunCore(record, logger);
            if (!canProceed)
            {
                return record.Value;
            }

            SkuCatalog.CloudEnvironmentSkus.TryGetValue(record.Value.SkuName, out var sku);
            if (sku.ComputeOS == ComputeOS.Windows || !string.IsNullOrEmpty(record.Value.SubnetResourceId))
            {
                // Windows can only be queued resume because the VM has to be constructed from the given OS disk.
                await UpdateStateAsync(
                    record,
                    CloudEnvironmentState.Queued,
                    reason: string.Empty,
                    trigger: null,
                    logger);
            }
            else
            {
                var isArchivedEnvironment = record.Value.Storage.Type == ResourceType.StorageArchive;
                var startingStateReason = isArchivedEnvironment ? MessageCodes.RestoringFromArchive.ToString() : null;
                await UpdateStateAsync(
                    record,
                    CloudEnvironmentState.Starting,
                    startingStateReason,
                    CloudEnvironmentStateUpdateTriggers.StartEnvironment,
                    logger);
            }

            // Run resume action.
            return await EnvironmentResumeAction.RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override bool IsEnvironmentInTargetState(CloudEnvironmentState cloudEnvironmentState)
        {
            return cloudEnvironmentState == CloudEnvironmentState.Starting || cloudEnvironmentState == CloudEnvironmentState.Queued;
        }
    }
}
