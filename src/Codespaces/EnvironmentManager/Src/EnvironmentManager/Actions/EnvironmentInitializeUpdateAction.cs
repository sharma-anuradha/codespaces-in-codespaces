// <copyright file="EnvironmentInitializeUpdateAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    public class EnvironmentInitializeUpdateAction : EnvironmentBaseIntializeStartAction<EnvironmentUpdateActionInput>, IEnvironmentInitializeUpdateAction
    {
        public EnvironmentInitializeUpdateAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            IEnvironmentUpdateAction environmentUpdateAction,
            ISkuCatalog skuCatalog,
            ISkuUtils skuUtils,
            IPlanManager planManager,
            ISubscriptionManager subscriptionManager,
            IEnvironmentSubscriptionManager environmentSubscriptionManager,
            EnvironmentManagerSettings environmentManagerSettings)
            : base(
                  environmentStateManager,
                  repository,
                  currentLocationProvider,
                  currentUserProvider,
                  controlPlaneInfo,
                  environmentAccessManager,
                  skuCatalog,
                  skuUtils,
                  planManager,
                  subscriptionManager,
                  environmentSubscriptionManager,
                  environmentManagerSettings)
        {
            EnvironmentUpdateAction = environmentUpdateAction;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_initialize_update_action";

        private IEnvironmentUpdateAction EnvironmentUpdateAction { get; }

        /// <inheritdoc/>
        public Task<CloudEnvironment> RunAsync(
            Guid environmentId,
            CloudEnvironmentParameters cloudEnvironmentParams,
            IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            logger.AddEnvironmentId(environmentId.ToString());

            var input = new EnvironmentUpdateActionInput(environmentId)
            {
                CloudEnvironmentParameters = cloudEnvironmentParams,
            };

            return RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(
            EnvironmentUpdateActionInput input,
            object transientState,
            IDiagnosticsLogger logger)
        {
            var record = await FetchAsync(input, logger);

            if (!await ConfigureRunCoreAsync(record, logger))
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
                    CloudEnvironmentState.Exporting,
                    startingStateReason,
                    CloudEnvironmentStateUpdateTriggers.ExportEnvironment,
                    logger);
            }

            // Run update action
            return await EnvironmentUpdateAction.RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override bool IsEnvironmentInTargetState(CloudEnvironmentState cloudEnvironmentState)
        {
            return cloudEnvironmentState == CloudEnvironmentState.Updating || cloudEnvironmentState == CloudEnvironmentState.Queued;
        }
    }
}
