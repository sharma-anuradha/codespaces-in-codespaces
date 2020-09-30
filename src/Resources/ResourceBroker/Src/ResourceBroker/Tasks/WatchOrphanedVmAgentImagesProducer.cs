// <copyright file="WatchOrphanedVmAgentImagesProducer.cs" company="Microsoft">
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
    /// Watch orphan vm agent images producer
    /// </summary>
    public class WatchOrphanedVmAgentImagesProducer : BaseResourceImageProducer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedVmAgentImagesProducer"/> class.
        /// </summary>
        /// <param name="controlPlaneAzureResourceAccessor">Gets control plan Azure resource accessor.</param>
        /// <param name="controlPlaneInfo">Gets control plan info.</param>
        /// <param name="jobSchedulerFeatureFlags">Gets job Scheduler Feature Flags</param>
        public WatchOrphanedVmAgentImagesProducer(
           IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
           IControlPlaneInfo controlPlaneInfo,
           IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
            : base(jobSchedulerFeatureFlags)
        {
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            ControlPlaneAzureResourceAccessor = Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));
        }

        protected override string JobName => "watch_orphaned_vm_agent_image_task";

        protected override Type JobHandlerType => typeof(WatchOrphanedVmAgentImagesJobHandler);

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
                var (accountName, accountKey) = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeVmAgentImagesAsync(location);
                var account = new ShareConnectionInfo();
                account.StorageAccountName = accountName;
                account.StorageAccountKey = accountKey;
                accounts.Add(account);
            }

            return accounts;
        }
    }
}
