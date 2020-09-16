// <copyright file="WatchOrphanedStorageImagesProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Watch orphan storage images producer
    /// </summary>
    public class WatchOrphanedStorageImagesProducer : BaseResourceImageProducer
    {
        public WatchOrphanedStorageImagesProducer(
           IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
           IControlPlaneInfo controlPlaneInfo,
           IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
            : base(jobSchedulerFeatureFlags)
        {
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            ControlPlaneAzureResourceAccessor = Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));
        }

        protected override string JobName => "watch_orphaned_storage_image_task";

        protected override Type JobHandlerType => typeof(WatchOrphanedStorageImagesJobHandler);

        /// <summary>
        /// Gets controPlane info.
        /// </summary>
        private IControlPlaneInfo ControlPlaneInfo { get; }

        /// <summary>
        /// Gets Control plane accessor.
        /// </summary>
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
    }
}
