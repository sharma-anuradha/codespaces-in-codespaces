// <copyright file="ConfirmOrphanedSystemResourceJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.Constants;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    public class ConfirmOrphanedSystemResourceJobHandler : JobHandlerPayloadBase<ConfirmOrphanedSystemResourcePayload>, IJobHandlerTarget
    {
        public ConfirmOrphanedSystemResourceJobHandler(
            ICloudEnvironmentRepository environmentRepository,
            ISecretStoreRepository secretStoreRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBroker,
            IControlPlaneInfo controlPlane)
        {
            EnvironmentRepository = environmentRepository;
            SecretStoreRepository = secretStoreRepository;
            ResourceBroker = resourceBroker;
            ControlPlane = controlPlane;
        }

        /// <inheritdoc/>
        public IJobHandler JobHandler => this;

        /// <inheritdoc/>
        public string QueueId => JobQueueIds.ConfirmOrphanedSystemResourceJob;

        /// <inheritdoc/>
        public AzureLocation? Location => ControlPlane.Stamp.Location;

        private ICloudEnvironmentRepository EnvironmentRepository { get; }

        private ISecretStoreRepository SecretStoreRepository { get; }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBroker { get; }

        private IControlPlaneInfo ControlPlane { get; }

        protected override Task HandleJobAsync(ConfirmOrphanedSystemResourcePayload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                "handle_confirm_orphaned_system_resource",
                async (childLogger) =>
                {
                    var resourceId = payload.ResourceId;
                    var resourceType = payload.ResourceType;

                    childLogger
                        .FluentAddBaseValue("ResourceId", resourceId)
                        .FluentAddBaseValue("ResourceType", resourceType);

                    var resourceIsReferenced = await CheckResourceReferencesByTypeAsync(resourceId, resourceType, childLogger);           
                    if (!resourceIsReferenced)
                    {
                        childLogger.FluentAddBaseValue("ResourceIsOrphaned", true);

                        await ResourceBroker.DeleteAsync(Guid.Empty, Guid.Parse(resourceId), childLogger.NewChildLogger());                        
                    }
                });
        }

        private Task<bool> CheckResourceReferencesByTypeAsync(string resourceId, ResourceType resourceType, IDiagnosticsLogger logger)
        {
            switch (resourceType)
            {
                case ResourceType.ComputeVM:
                case ResourceType.StorageFileShare:
                case ResourceType.StorageArchive:
                case ResourceType.OSDisk:
                case ResourceType.Snapshot:
                    return CheckResourceEnvironmentReferencesByTypeAsync(resourceId, resourceType, logger);

                case ResourceType.KeyVault:
                    return CheckResourceSecretStoreReferencesAsync(resourceId, logger);

                default:
                    throw new ArgumentException($"Resource Type {resourceType} is not handled");
            }
        }

        private async Task<bool> CheckResourceEnvironmentReferencesByTypeAsync(string resourceId, ResourceType resourceType, IDiagnosticsLogger logger)
        {
            var environment = await EnvironmentRepository.GetEnvironmentUsingResource(resourceId, resourceType, logger.NewChildLogger());

            var resourceIsReferenced = environment != null;

            if (!resourceIsReferenced)
            {
                logger.FluentAddBaseValue("EnvironmentReferencingResource", environment.Id);
            }

            return resourceIsReferenced;
        }

        private async Task<bool> CheckResourceSecretStoreReferencesAsync(string resourceId, IDiagnosticsLogger logger)
        {
            var secretStore = await SecretStoreRepository.GetSecretStoreUsingResource(resourceId, logger.NewChildLogger());

            var resourceIsReferenced = secretStore != null;

            if (resourceIsReferenced)
            {
                logger.FluentAddBaseValue("SecretStoreReferencingResource", secretStore.Id);
            }

            return resourceIsReferenced;
        }
    }
}
