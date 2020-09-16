// <copyright file="WatchOrphanedStorageImagesTask.cs" company="Microsoft">
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
    /// WatchOrphanedArtifactImagesTask to delete artifacts(kitchen sink images/blobs).
    /// </summary>
    public class WatchOrphanedStorageImagesTask : BaseResourceImageTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedStorageImagesTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Gets storage accounts.</param>
        /// <param name="controlPlaneInfo">Gets control plan info.</param>
        /// <param name="skuCatalog">Gets skuCatalog that has active image info.</param>
        /// <param name="jobSchedulerFeatureFlags">The job scheduler feature flags instance.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        public WatchOrphanedStorageImagesTask(
            ResourceBrokerSettings resourceBrokerSettings,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            IControlPlaneInfo controlPlaneInfo,
            ISkuCatalog skuCatalog,
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags,
            IConfigurationReader configurationReader)
            : base(
                  resourceBrokerSettings,
                  taskHelper,
                  claimedDistributedLease,
                  resourceNameBuilder,
                  jobSchedulerFeatureFlags,
                  configurationReader)
        {
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            ControlPlaneAzureResourceAccessor = Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));
        }

        /// <inheritdoc/>
        protected override string TaskName { get; } = nameof(WatchOrphanedStorageImagesTask);

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => "WatchOrphanedStorageImagesTask";

        /// <inheritdoc/>
        protected override string LogBaseName { get; } = ResourceLoggingConstants.WatchOrphanedStorageImagesTask;

        /// <inheritdoc/>
        protected override DateTime CutOffTime => DateTime.Now.AddMonths(-1).ToUniversalTime();

        /// <summary>
        /// Gets the SkuCatalog to access the active image info.
        /// </summary>
        protected ISkuCatalog SkuCatalog { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IControlPlaneAzureResourceAccessor ControlPlaneAzureResourceAccessor { get; }

        /// <inheritdoc/>
        protected override async Task<IEnumerable<ShareConnectionInfo>> GetStorageAccountsAsync()
        {
            var locations = ControlPlaneInfo.Stamp.DataPlaneLocations;
            var accounts = new List<ShareConnectionInfo>();

            foreach (var location in locations)
            {
                var (accountName, accountKey) = await ControlPlaneAzureResourceAccessor
                        .GetStampStorageAccountForStorageImagesAsync(location);
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
            foreach (var item in SkuCatalog.BuildArtifactStorageImageFamilies.Values)
            {
                activeImages.Add(await item.GetCurrentImageNameAsync(logger));
            }

            return activeImages;
        }

        /// <inheritdoc/>
        protected override string GetContainerName()
        {
            return ControlPlaneInfo.FileShareTemplateContainerName;
        }

        /// <inheritdoc/>
        protected override IEnumerable<ImageFamilyType> GetArtifactTypesToCleanup()
        {
            return new List<ImageFamilyType> { ImageFamilyType.Storage, };
        }

        /// <inheritdoc/>
        protected override int GetMinimumBlobCountToBeRetained() => 15;

        /// <inheritdoc/>
        protected override ImageFamilyType GetImageFamilyType() => ImageFamilyType.Storage;
    }
}
