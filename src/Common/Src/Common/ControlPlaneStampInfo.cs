// <copyright file="ControlPlaneStampInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// The azure secret provider.
    /// </summary>
    public class ControlPlaneStampInfo : IControlPlaneStampInfo
    {
        private const int AzureStorageAccountNameLengthMax = 24;
        private const int StorageAccountUniquePrefixLengthMax = AzureStorageAccountNameLengthMax - 10;
        private const string ComputeQueueKind = "cq";
        private const string StorageImageKind = "si";
        private const string VmAgentImageKind = "vm";

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlPlaneStampInfo"/> class.
        /// </summary>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="location">The control plane azure location.</param>
        /// <param name="storageAccountUniquePrefix">A globally-unique prefix for storage accounts.</param>
        /// <param name="controlPlaneStampSettings">The control plane stamp settings.</param>
        public ControlPlaneStampInfo(
            IControlPlaneInfo controlPlaneInfo,
            AzureLocation location,
            string storageAccountUniquePrefix,
            ControlPlaneStampSettings controlPlaneStampSettings)
        {
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNullOrEmpty(storageAccountUniquePrefix, nameof(storageAccountUniquePrefix));
            Requires.Argument(storageAccountUniquePrefix.Length <= StorageAccountUniquePrefixLengthMax, nameof(storageAccountUniquePrefix), $"The prefix '{nameof(storageAccountUniquePrefix)}' must not be longer than {StorageAccountUniquePrefixLengthMax} characters.");
            Requires.Argument(storageAccountUniquePrefix.ToCharArray().All(c => char.IsLetterOrDigit(c)), nameof(storageAccountUniquePrefix), $"The prefix '{nameof(storageAccountUniquePrefix)}' contains invalid characters.");
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Location = location;
            StorageAccountUniquePrefix = storageAccountUniquePrefix;
            ControlPlaneStampSettings = Requires.NotNull(controlPlaneStampSettings, nameof(ControlPlaneStampSettings));
        }

        /// <inheritdoc/>
        public AzureLocation Location { get; }

        /// <inheritdoc/>
        public string StampResourceGroupName =>
            $"{ControlPlaneInfo.InstanceResourceGroupName}-{NotNullOrWhiteSpace(ControlPlaneStampSettings.StampName, nameof(ControlPlaneStampSettings.StampName))}";

        /// <inheritdoc/>
        public string DnsHostName => NotNullOrWhiteSpace(ControlPlaneStampSettings.DnsHostName, nameof(ControlPlaneStampSettings.DnsHostName));

        /// <inheritdoc/>
        public IEnumerable<AzureLocation> DataPlaneLocations => ControlPlaneStampSettings.DataPlaneLocations;

        /// <inheritdoc/>
        public string StampCosmosDbAccountName =>
            $"{StampResourceGroupName}-db";

        /// <inheritdoc/>
        public string StampStorageAccountName =>
            $"{StampResourceGroupName}-sa".Replace("-", string.Empty).ToLowerInvariant();

        /// <remarks>
        /// This map will need to grow as we add new supported data-plane locations.
        /// </remarks>
        private static Dictionary<AzureLocation, string> StorageAccountRegionCodes { get; } = new Dictionary<AzureLocation, string>
        {
            { AzureLocation.EastUs, "use" },
            { AzureLocation.SouthEastAsia, "asse" },
            { AzureLocation.WestEurope, "euw" },
            { AzureLocation.WestUs2, "usw2" },
        };

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ControlPlaneStampSettings ControlPlaneStampSettings { get; }

        private string StorageAccountUniquePrefix { get; }

        /// <inheritdoc/>
        public string GetStampStorageAccountNameForComputeQueues(AzureLocation computeVmLocation)
        {
            return MakeStorageAccountName(ComputeQueueKind, computeVmLocation);
        }

        /// <inheritdoc/>
        public string GetStampStorageAccountNameForComputeVmAgentImages(AzureLocation computeVmLocation)
        {
            return MakeStorageAccountName(VmAgentImageKind, computeVmLocation);
        }

        /// <inheritdoc/>
        public string GetStampStorageAccountNameForStorageImages(AzureLocation computeStorageLocation)
        {
            return MakeStorageAccountName(StorageImageKind, computeStorageLocation);
        }

        private static string NotNullOrWhiteSpace(string value, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"The property {nameof(ControlPlaneStampSettings)}.{propertyName} is required.");
            }

            return value;
        }

        private string MakeStorageAccountName(string kind, AzureLocation azureLocation)
        {
            Requires.Argument(kind.Length <= 2, nameof(kind), $"The storage kind '{kind}' must not be longer than 2 characters.");

            if (!DataPlaneLocations.Contains(azureLocation))
            {
                throw new NotSupportedException($"The data-plane location '{azureLocation}' is not supported in stamp '{this.StampResourceGroupName}'");
            }

            var regionCode = StorageAccountRegionCodes[azureLocation];
            var accountName = $"{StorageAccountUniquePrefix}-{ControlPlaneStampSettings.StampName}-{kind}-{regionCode}".Replace("-", string.Empty).ToLowerInvariant();
            if (accountName.Length > AzureStorageAccountNameLengthMax)
            {
                throw new InvalidOperationException($"The resulting storage account name '{accountName}' must not be longer than {AzureStorageAccountNameLengthMax} characters.");
            }

            return accountName;
        }
    }
}
