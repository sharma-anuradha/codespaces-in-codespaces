// <copyright file="ResourceSelectorFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Resource selector.
    /// </summary>
    public class ResourceSelectorFactory : IResourceSelectorFactory
    {
        private const bool DefaultWindowsOSDiskPersistenceEnabled = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceSelectorFactory"/> class.
        /// </summary>
        /// <param name="skuCatalog">Sku catalog.</param>
        /// <param name="systemConfiguration">System configuration.</param>
        public ResourceSelectorFactory(ISkuCatalog skuCatalog, ISystemConfiguration systemConfiguration)
        {
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            SystemConfiguration = Requires.NotNull(systemConfiguration, nameof(systemConfiguration));
        }

        private ISkuCatalog SkuCatalog { get; }

        private ISystemConfiguration SystemConfiguration { get; }

        /// <inheritdoc/>
        public async Task<IList<AllocateRequestBody>> CreateAllocationRequestsAsync(CloudEnvironment cloudEnvironment, CloudEnvironmentOptions cloudEnvironmentOptions, IDiagnosticsLogger logger)
        {
            SkuCatalog.CloudEnvironmentSkus.TryGetValue(cloudEnvironment.SkuName, out var sku);

            var requests = new List<AllocateRequestBody>();

            var properties = new AllocateExtendedProperties()
            {
                AllocationRequestID = cloudEnvironment.Id,
                SubnetResourceId = cloudEnvironment.SubnetResourceId,
            };

            var isWindowsEnvPersistingOSDisk = await IsWindowsEnvironmentPersistingOSDiskAsync(logger);
            var isOsDiskAllocationRequired = isWindowsEnvPersistingOSDisk && sku.ComputeOS == ComputeOS.Windows;
            var isStorageAllocated = cloudEnvironment.Storage?.Type == ResourceType.StorageFileShare;

            if (isOsDiskAllocationRequired)
            {
                properties.OSDiskResourceID = cloudEnvironment.OSDisk?.ResourceId.ToString();

                // Note: Since this method could be called in two ways
                // 1) Straight from web-request. In that case the transitions are correctly preserved. So looking at the last transition would do.
                // 2) Via continuations. Resuming will have already added Initializing and InProgress states for the current continuation, so we just look back till we find a success or failed/cancelled.
                if (cloudEnvironment?.Transitions?.Resuming?.StatusChanges != default)
                {
                    var pastStatus = cloudEnvironment.Transitions.Resuming.StatusChanges.OrderByDescending(x => x.Time).Select(x => x.Status);
                    foreach (var status in pastStatus)
                    {
                        if (status == Common.Continuation.OperationState.Succeeded)
                        {
                            break;
                        }
                        else if (status == Common.Continuation.OperationState.Failed || status == Common.Continuation.OperationState.Cancelled)
                        {
                            properties.HardBoot = true;
                            break;
                        }
                    }
                }
            }

            var queueComputeRequest = cloudEnvironmentOptions.QueueResourceAllocation || !string.IsNullOrWhiteSpace(properties.SubnetResourceId);
            var computeRequest = new AllocateRequestBody
            {
                Type = ResourceType.ComputeVM,
                SkuName = cloudEnvironment.SkuName,
                Location = cloudEnvironment.Location,
                QueueCreateResource = queueComputeRequest,
                ExtendedProperties = properties,
            };

            requests.Add(computeRequest);

            if (isOsDiskAllocationRequired)
            {
                // Only allocate a new disk if we don't have a snapshot
                if (cloudEnvironment.OSDiskSnapshot == default)
                {
                    var osDiskRequest = new AllocateRequestBody
                    {
                        Type = ResourceType.OSDisk,
                        SkuName = cloudEnvironment.SkuName,
                        Location = cloudEnvironment.Location,
                        QueueCreateResource = queueComputeRequest,
                        ExtendedProperties = properties,
                    };

                    requests.Add(osDiskRequest);
                }
            }

            if (!(isOsDiskAllocationRequired || isStorageAllocated))
            {
                var storageRequest = new AllocateRequestBody
                {
                    Type = ResourceType.StorageFileShare,
                    SkuName = cloudEnvironment.SkuName,
                    Location = cloudEnvironment.Location,
                    QueueCreateResource = false, // Note: storage always from the hot pool.
                    ExtendedProperties = properties,
                };

                requests.Add(storageRequest);
            }

            return requests;
        }

        private async Task<bool> IsWindowsEnvironmentPersistingOSDiskAsync(IDiagnosticsLogger logger)
        {
            return await SystemConfiguration.GetValueAsync("featureflag:windows-osdisk-persistence-enabled", logger, DefaultWindowsOSDiskPersistenceEnabled);
        }
    }
}
