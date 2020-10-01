// <copyright file="ConfirmOrphanedSystemResourceJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
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
        private static readonly ResourceType[] DefaultEnabledResourceTypesToDelete = new[]
        {
             ResourceType.ComputeVM,
             ResourceType.KeyVault,
        };

        private static readonly string DefaultEnabledResourceTypesToDeleteConfigValue =
            string.Join(",", DefaultEnabledResourceTypesToDelete.Select((t) => t.ToString()));

        public ConfirmOrphanedSystemResourceJobHandler(
            ICloudEnvironmentRepository environmentRepository,
            ISecretStoreRepository secretStoreRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBroker,
            IControlPlaneInfo controlPlane,
            IConfigurationReader configurationReader)
        {
            EnvironmentRepository = environmentRepository;
            SecretStoreRepository = secretStoreRepository;
            ResourceBroker = resourceBroker;
            ControlPlane = controlPlane;
            ConfigurationReader = configurationReader;
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

        private IConfigurationReader ConfigurationReader { get; }

        private string ConfigurationBaseName => "ConfirmOrphanedSystemResourceJobHandler";

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

                    // Don't pass NewChildLogger here as it will be adding properties to the logger
                    var resourceIsReferenced = await CheckResourceReferencesByTypeAsync(resourceId, resourceType, childLogger);

                    childLogger.FluentAddBaseValue("ResourceIsReferenced", resourceIsReferenced);

                    if (!resourceIsReferenced)
                    {
                        var isDeleteEnabled = await CanDeleteResourceType(resourceType, childLogger.NewChildLogger());

                        childLogger.FluentAddBaseValue("ResourceDeleteAttempted", isDeleteEnabled);

                        if (isDeleteEnabled)
                        {
                            await ResourceBroker.DeleteAsync(Guid.Empty, Guid.Parse(resourceId), childLogger.NewChildLogger());
                        }
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

            if (resourceIsReferenced)
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

        private async Task<bool> CanDeleteResourceType(ResourceType resourceType, IDiagnosticsLogger logger)
        {
            var enabledResourceTypes = await this.ConfigurationReader.ReadSettingAsync(ConfigurationBaseName, "enabled-resource-types", logger.NewChildLogger(), DefaultEnabledResourceTypesToDeleteConfigValue);
            if (enabledResourceTypes == default)
            {
                // No resource types are enabled
                return false;
            }

            var enabledResourceTypeList = enabledResourceTypes.Split(',').Select(type => type.Trim());

            var resourceTypeString = resourceType.ToString();
            var isEnabled = enabledResourceTypeList.Any((enabledType) => string.Equals(enabledType, resourceTypeString, StringComparison.OrdinalIgnoreCase));

            return isEnabled;
        }
    }
}
