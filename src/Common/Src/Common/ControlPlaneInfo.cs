// <copyright file="ControlPlaneInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// The azure secret provider.
    /// </summary>
    public class ControlPlaneInfo : IControlPlaneInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlPlaneInfo"/> class.
        /// </summary>
        /// <param name="options">The azure resource provider options.</param>
        /// <param name="currentLocationProvider">The current stamp location provider.</param>
        /// <param name="resourceNameBuilder">The resource name builder.</param>
        public ControlPlaneInfo(
            IOptions<ControlPlaneInfoOptions> options,
            ICurrentLocationProvider currentLocationProvider,
            IResourceNameBuilder resourceNameBuilder)
        {
            ControlPlaneSettings = Requires.NotNull(options?.Value?.ControlPlaneSettings, nameof(ControlPlaneSettings));
            Requires.NotNull(currentLocationProvider, nameof(currentLocationProvider));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));

            // Select the stamp settings for the current control plane.
            var controlPlaneLocation = currentLocationProvider.CurrentLocation;
            if (!ControlPlaneSettings.Stamps.TryGetValue(controlPlaneLocation, out var stampSettings))
            {
                throw new InvalidOperationException($"No stamp is defined for the control-plane location '{controlPlaneLocation}'.");
            }

            RegionCode = ControlPlaneStampInfo.RegionCodes[controlPlaneLocation];

            var stampShortUniquePrefix = StampShortUniquePrefix;
            Stamp = new ControlPlaneStampInfo(this, controlPlaneLocation, stampShortUniquePrefix, stampSettings);

            AllStamps = new ReadOnlyDictionary<AzureLocation, IControlPlaneStampInfo>(ControlPlaneSettings.Stamps
                .ToDictionary(
                    item => item.Key,
                    item => (IControlPlaneStampInfo)new ControlPlaneStampInfo(this, item.Key, stampShortUniquePrefix, item.Value)));

            // Global configuraiton validation for all stamps.
            ValidateDataPlaneLocationsHaveSingleOwningControlPlane();
        }

        /// <inheritdoc/>
        public string EnvironmentResourceGroupName =>
            $"{NotNullOrWhiteSpace(ControlPlaneSettings.Prefix, nameof(ControlPlaneSettings.Prefix))}-{NotNullOrWhiteSpace(ControlPlaneSettings.ServiceName, nameof(ControlPlaneSettings.ServiceName))}-{NotNullOrWhiteSpace(ControlPlaneSettings.EnvironmentName, nameof(ControlPlaneSettings.EnvironmentName))}";

        /// <inheritdoc/>
        public string EnvironmentKeyVaultName =>
            $"{EnvironmentResourceGroupName}-kv";

        /// <inheritdoc/>
        public string InstanceResourceGroupName =>
            $"{EnvironmentResourceGroupName}-{NotNullOrWhiteSpace(ControlPlaneSettings.InstanceName, nameof(ControlPlaneSettings.InstanceName))}";

        /// <inheritdoc/>
        public string GlobalCosmosDbAccountName =>
            $"{InstanceResourceGroupName}-db";

        /// <inheritdoc/>
        public string RegionalCosmosDbAccountName =>
            $"{InstanceResourceGroupName}-{RegionCode}-regional-db";

        /// <inheritdoc/>
        public string ResourcesGlobalCosmosDbAccountName =>
            $"{InstanceResourceGroupName}-resources-db";

        /// <inheritdoc/>
        public string InstanceMapsAccountName =>
            $"{InstanceResourceGroupName}-maps";

        /// <inheritdoc/>
        public string DnsHostName => NotNullOrWhiteSpace(ControlPlaneSettings.DnsHostName, nameof(ControlPlaneSettings.DnsHostName));

        /// <inheritdoc/>
        public string VirtualMachineAgentContainerName => NotNullOrWhiteSpace(ResourceNameBuilder.GetVirtualMachineAgentContainerName(ControlPlaneSettings.VirtualMachineAgentContainerName), nameof(ControlPlaneSettings.VirtualMachineAgentContainerName));

        /// <inheritdoc/>
        public string FileShareTemplateContainerName => NotNullOrWhiteSpace(ControlPlaneSettings.FileShareTemplateContainerName, nameof(ControlPlaneSettings.FileShareTemplateContainerName));

        /// <inheritdoc/>
        public IControlPlaneStampInfo Stamp { get; }

        /// <inheritdoc/>
        public IReadOnlyDictionary<AzureLocation, IControlPlaneStampInfo> AllStamps { get; }

        private ControlPlaneSettings ControlPlaneSettings { get; }

        private string RegionCode { get; }

        /// <summary>
        /// Gets the resource name builder.
        /// </summary>
        private IResourceNameBuilder ResourceNameBuilder { get; }

        /// <summary>
        /// Gets a name like "vsodevci", "vsopperel", etc.
        /// This doesn't use the normal prefix-service pattern because storage names would be too long.
        /// </summary>
        private string StampShortUniquePrefix =>
            NotNullOrWhiteSpace(ControlPlaneSettings.ShortPrefix, nameof(ControlPlaneSettings.ShortPrefix)) +
            NotNullOrWhiteSpace(ControlPlaneSettings.EnvironmentName, nameof(ControlPlaneSettings.EnvironmentName)) +
            NotNullOrWhiteSpace(ControlPlaneSettings.InstanceName, nameof(ControlPlaneSettings.InstanceName))
            .ToLowerInvariant();

        /// <inheritdoc/>
        public bool TryGetSubscriptionId(out string subscriptonId)
        {
            subscriptonId = ControlPlaneSettings.SubscriptionId;
            return !string.IsNullOrEmpty(subscriptonId);
        }

        /// <inheritdoc/>
        public IEnumerable<AzureLocation> GetAllDataPlaneLocations()
        {
            return AllStamps.Values.SelectMany(s => s.DataPlaneLocations).Distinct();
        }

        /// <inheritdoc/>
        public IControlPlaneStampInfo GetOwningControlPlaneStamp(AzureLocation dataPlaneLocation)
        {
            try
            {
                return AllStamps.Values.Single(stamp => stamp.DataPlaneLocations.Contains(dataPlaneLocation));
            }
            catch (Exception ex)
            {
                throw new NotSupportedException($"Unsupported location: {dataPlaneLocation}", ex);
            }
        }

        private static string NotNullOrWhiteSpace(string value, string propertyName)
        {
           return CommonUtils.NotNullOrWhiteSpace(value, propertyName, nameof(Contracts.ControlPlaneSettings));
        }

        private void ValidateDataPlaneLocationsHaveSingleOwningControlPlane()
        {
            // As a global configuration check, validate that each data-plane location is owned by only a single control-plane stamp.
            var dataPlaneToControlPlaneMap = new Dictionary<AzureLocation, IControlPlaneStampInfo>();
            foreach (var item in AllStamps)
            {
                var controlPlaneLocation = item.Key;
                var controlPlaneStampInfo = item.Value;

                foreach (var dataPlaneLocation in controlPlaneStampInfo.DataPlaneLocations)
                {
                    if (dataPlaneToControlPlaneMap.TryGetValue(dataPlaneLocation, out var otherStamp))
                    {
                        throw new InvalidOperationException($"The data plane location '{dataPlaneLocation}' is declared in both {controlPlaneStampInfo.StampResourceGroupName} and {otherStamp.StampResourceGroupName}.");
                    }

                    dataPlaneToControlPlaneMap[dataPlaneLocation] = controlPlaneStampInfo;
                }
            }
        }
    }
}
