// <copyright file="ExportStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VsSaaS.Azure.Metrics;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider
{
    /// <inheritdoc/>
    public class ExportStorageProvider : SharedStorageProvider, IExportStorageProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportStorageProvider"/> class.
        /// </summary>
        /// <param name="capacityManager">The capacity manager.</param>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        /// <param name="azureClientFactory">The azure client factory.</param>
        /// <param name="metricsProvider">The azure metrics provider.</param>
        /// <param name="resourceNameBuilder">Resource naming for DEV stamps.</param>
        /// <param name="personalStampSettings">DEV stamp settings.</param>
        /// <param name="diagnosticsLoggerFactory">The diagnostics logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public ExportStorageProvider(
            ICapacityManager capacityManager,
            IControlPlaneInfo controlPlaneInfo,
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            IAzureClientFactory azureClientFactory,
            IMetricsProvider metricsProvider,
            IResourceNameBuilder resourceNameBuilder,
            DeveloperPersonalStampSettings personalStampSettings,
            IDiagnosticsLoggerFactory diagnosticsLoggerFactory,
            LogValueSet defaultLogValues)
            : base(capacityManager, controlPlaneInfo, azureSubscriptionCatalog, azureClientFactory, metricsProvider, resourceNameBuilder, personalStampSettings, diagnosticsLoggerFactory, defaultLogValues)
        {
            Requires.NotNull(capacityManager, nameof(capacityManager));
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            Requires.NotNull(azureClientFactory, nameof(azureClientFactory));
            Requires.NotNull(metricsProvider, nameof(metricsProvider));
            Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
            Requires.NotNull(personalStampSettings, nameof(personalStampSettings));
            Requires.NotNull(diagnosticsLoggerFactory, nameof(diagnosticsLoggerFactory));
            Requires.NotNull(defaultLogValues, nameof(defaultLogValues));
        }

        /// <inheritdoc/>
        protected override SkuName StorageAccountSkuName => SkuName.StandardZRS;

        /// <inheritdoc/>
        protected override int StorageAccountsPerRegionPerSubscription => 10;

        /// <inheritdoc/>
        protected override double StorageAccountMaxCapacityInGb => 5000000; // 5PB

        /// <inheritdoc/>
        protected override string StorageInitStorageAccountMessageLog => "export_storage_init_storage_account";

        /// <inheritdoc/>
        protected override string StorageCapacityMessageLog => "export_storage_capacity";

        /// <inheritdoc/>
        public Task<ISharedStorageInfo> GetExportStorageAccountAsync(
            AzureLocation location,
            int minimumRequiredGB,
            IDiagnosticsLogger logger,
            bool forceCapacityCheck = false)
        {
            return GetStorageAccountAsync(location, minimumRequiredGB, logger, forceCapacityCheck);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ISharedStorageInfo>> ListExportStorageAccountsAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            return ListStorageAccountsAsync(location, logger);
        }

        /// <inheritdoc/>
        public override string GetStorageAccountName(AzureLocation location, int index)
        {
            var storageAccountName = ControlPlaneInfo.Stamp.GetDataPlaneStorageAccountNameForExportStorageName(location, index);
            storageAccountName = ResourceNameBuilder.GetStorageAccountName(storageAccountName, "ex");
            return storageAccountName;
        }
    }
}
