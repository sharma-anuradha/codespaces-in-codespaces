// <copyright file="WatchOrphanedStorageImagesTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// WatchOrphanedArtifactImagesTask to delete artifacts(kitchen sink images/blobs).
    /// </summary>
    public class WatchOrphanedStorageImagesTask : BaseResourceImageTask, IWatchOrphanedStorageImagesTask
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
        public WatchOrphanedStorageImagesTask(
            ResourceBrokerSettings resourceBrokerSettings,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            IControlPlaneInfo controlPlaneInfo,
            ISkuCatalog skuCatalog)
            : base(
                  resourceBrokerSettings,
                  taskHelper,
                  claimedDistributedLease,
                  resourceNameBuilder,
                  controlPlaneInfo,
                  controlPlaneAzureResourceAccessor,
                  skuCatalog)
        {
        }

        /// <inheritdoc/>
        protected override string TaskName { get; } = nameof(WatchOrphanedStorageImagesTask);

        /// <inheritdoc/>
        protected override string LogBaseName { get; } = ResourceLoggingConstants.WatchOrphanedStorageImagesTask;

        /// <inheritdoc/>
        protected override DateTime CutOffDate => DateTime.Now.AddMonths(-3);

        /// <inheritdoc/>
        protected override IEnumerable<ImageFamilyType> GetArtifactTypesToCleanup()
        {
            return new List<ImageFamilyType> { ImageFamilyType.Storage, };
        }

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
    }
}
