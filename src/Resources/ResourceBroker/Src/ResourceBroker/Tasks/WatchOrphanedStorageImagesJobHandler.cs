// <copyright file="WatchOrphanedStorageImagesJobHandler.cs" company="Microsoft">
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
    /// Job handler to delete storage images.
    /// </summary>
    public class WatchOrphanedStorageImagesJobHandler : BaseResourceImageJobHandler<WatchOrphanedStorageImagesJobHandler>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedStorageImagesJobHandler"/> class.
        /// </summary>
        /// <param name="controlPlaneInfo">Gets control plan info.</param>
        /// <param name="skuCatalog">Gets skuCatalog that has active image info.</param>
        public WatchOrphanedStorageImagesJobHandler(
           IControlPlaneInfo controlPlaneInfo,
           ISkuCatalog skuCatalog)
        {
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
        }

        /// <inheritdoc/>
        protected override ImageFamilyType ImageFamilyType => ImageFamilyType.Storage;

        /// <inheritdoc/>
        protected override string LogBaseName { get; } = ResourceLoggingConstants.WatchOrphanedStorageImagesTask;

        /// <inheritdoc/>
        protected override DateTime CutOffTime => DateTime.Now.AddMonths(-1).ToUniversalTime();

        /// <inheritdoc/>
        protected override int MinimumBlobCountToBeRetained => 15;

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
            return ControlPlaneInfo.FileShareTemplateContainerName;
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
    }
}