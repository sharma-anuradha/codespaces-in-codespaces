// <copyright file="StaticEnvironmentSku.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// SKU for Static Environments.
    /// </summary>
    public class StaticEnvironmentSku : ICloudEnvironmentSku
    {
        /// <summary>
        /// Gets name for Static Environments.
        /// </summary>
        public static string Name { get; } = "staticEnvironment";

        /// <inheritdoc/>
        public string SkuName { get; } = Name;

        /// <inheritdoc/>
        public string DisplayName { get; } = "Self-Hosted";

        /// <inheritdoc/>
        public IEnumerable<AzureLocation> SkuLocations { get; } = Enum.GetValues(typeof(AzureLocation)) as AzureLocation[];

        /// <inheritdoc/>
        public ComputeOS ComputeOS { get; } = default;

        /// <inheritdoc/>
        public string ComputeSkuFamily { get; } = Name;

        /// <inheritdoc/>
        public string ComputeSkuName { get; } = Name;

        /// <inheritdoc/>
        public string ComputeSkuSize { get; } = Name;

        /// <inheritdoc/>
        public int ComputeSkuCores { get; } = 0;

        /// <inheritdoc/>
        public IBuildArtifactImageFamily VmAgentImage { get; } = default;

        /// <inheritdoc/>
        public IVmImageFamily ComputeImage { get; } = default;

        /// <inheritdoc/>
        public string StorageSkuName { get; } = Name;

        /// <inheritdoc/>
        public IBuildArtifactImageFamily StorageImage { get; } = default;

        /// <inheritdoc/>
        public int StorageSizeInGB { get; } = 0;

        /// <inheritdoc/>
        public decimal StorageVsoUnitsPerHour { get; } = 0;

        /// <inheritdoc/>
        public decimal ComputeVsoUnitsPerHour { get; } = 0;

        /// <inheritdoc/>
        public int ComputePoolLevel { get; } = 0;

        /// <inheritdoc/>
        public int StoragePoolLevel { get; } = 0;

        /// <inheritdoc/>
        public bool IsExternalHardware { get; } = true;

        /// <inheritdoc/>
        public bool Enabled { get; } = true;

        /// <inheritdoc/>
        public SkuTier Tier => SkuTier.PremiumDSv3;
    }
}