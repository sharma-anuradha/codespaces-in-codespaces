// <copyright file="WatchOrphanedVmAgentImagesJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Job handler to delete artifact VSO agent images/blobs.
    /// </summary>
    public class WatchOrphanedVmAgentImagesJobHandler : BaseResourceImageJobHandler<WatchOrphanedVmAgentImagesJobHandler>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedVmAgentImagesJobHandler"/> class.
        /// </summary>
        /// <param name="controlPlaneInfo">Gets control plan info.</param>
        /// <param name="skuCatalog">Gets skuCatalog that has active image info.</param>
        public WatchOrphanedVmAgentImagesJobHandler(
           IControlPlaneInfo controlPlaneInfo,
           ISkuCatalog skuCatalog)
        {
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
        }

        /// <inheritdoc/>
        protected override ImageFamilyType ImageFamilyType => ImageFamilyType.VmAgent;

        /// <inheritdoc/>
        protected override string LogBaseName { get; } = ResourceLoggingConstants.WatchOrphanedVmAgentImagesTask;

        /// <inheritdoc/>
        protected override DateTime CutOffTime => DateTime.Now.AddMonths(-1).ToUniversalTime();

        /// <inheritdoc/>
        protected override int MinimumBlobCountToBeRetained => 10;

        /// <summary>
        /// Gets controPlane info.
        /// </summary>
        private IControlPlaneInfo ControlPlaneInfo { get; }

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
        protected override async Task<IEnumerable<string>> GetActiveImagesAsync(IDiagnosticsLogger logger)
        {
            var activeImages = new HashSet<string>();
            foreach (var item in SkuCatalog.BuildArtifactVmAgentImageFamilies.Values)
            {
                activeImages.Add(await item.GetCurrentImageNameAsync(logger));
            }

            return activeImages;
        }
    }
}