// <copyright file="WatchOrphanedVmAgentImagesTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// WatchOrphanedArtifactImagesTask to delete artifact VSO agent images/blobs.
    /// </summary>
    public class WatchOrphanedVmAgentImagesTask : BaseResourceImageTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedVmAgentImagesTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Gets storage accounts.</param>
        /// <param name="controlPlaneInfo">Gets control plan info.</param>
        /// <param name="skuCatalog">Gets skuCatalog that has active image info.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        public WatchOrphanedVmAgentImagesTask(
           ResourceBrokerSettings resourceBrokerSettings,
           ITaskHelper taskHelper,
           IClaimedDistributedLease claimedDistributedLease,
           IResourceNameBuilder resourceNameBuilder,
           IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
           IControlPlaneInfo controlPlaneInfo,
           ISkuCatalog skuCatalog,
           IConfigurationReader configurationReader)
            : base(
                  resourceBrokerSettings,
                  taskHelper,
                  claimedDistributedLease,
                  resourceNameBuilder,
                  configurationReader)
        {
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            ControlPlaneAzureResourceAccessor = Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));
        }

        /// <inheritdoc/>
        protected override string TaskName { get; } = nameof(WatchOrphanedVmAgentImagesTask);

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => "WatchOrphanedVmAgentImagesTask";

        /// <inheritdoc/>
        protected override string LogBaseName { get; } = ResourceLoggingConstants.WatchOrphanedVmAgentImagesTask;

        /// <inheritdoc/>
        protected override DateTime CutOffTime => DateTime.Now.AddMonths(-1).ToUniversalTime();

        /// <summary>
        /// Gets controPlane info.
        /// </summary>
        private IControlPlaneInfo ControlPlaneInfo { get; }

        /// <summary>
        /// Gets Control plane accessor.
        /// </summary>
        private IControlPlaneAzureResourceAccessor ControlPlaneAzureResourceAccessor { get; }

        /// <summary>
        /// Gets the SkuCatalog to access the active image info.
        /// </summary>
        private ISkuCatalog SkuCatalog { get; }

        /// <inheritdoc/>
        protected override string GetContainerName()
        {
            return ControlPlaneInfo.VirtualMachineAgentContainerName;
        }

        /// <inheritdoc/>
        protected override async Task<IEnumerable<ShareConnectionInfo>> GetStorageAccountsAsync()
        {
            var locations = ControlPlaneInfo.Stamp.DataPlaneLocations;
            var accounts = new List<ShareConnectionInfo>();

            foreach (var location in locations)
            {
                var (accountName, accountKey) = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeVmAgentImagesAsync(location);
                var account = new ShareConnectionInfo();
                account.StorageAccountName = accountName;
                account.StorageAccountKey = accountKey;
                accounts.Add(account);
            }

            return accounts;
        }

        /// <inheritdoc/>
        protected override async Task<IEnumerable<string>> GetActiveImagesAsync(IDiagnosticsLogger logger)
        {
            var activeImages = new HashSet<string>();
            foreach (var item in SkuCatalog.BuildArtifactVmAgentImageFamilies.Values)
            {
                activeImages.Add(await item.GetCurrentImageNameAsync(logger));
            }

            return activeImages;
        }

        /// <inheritdoc/>
        protected override IEnumerable<ImageFamilyType> GetArtifactTypesToCleanup()
        {
            return new List<ImageFamilyType> { ImageFamilyType.VmAgent, };
        }

        /// <inheritdoc/>
        protected override int GetMinimumBlobCountToBeRetained() => 10;

        /// <inheritdoc/>
        protected override ImageFamilyType GetImageFamilyType() => ImageFamilyType.VmAgent;
    }
}