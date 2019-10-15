﻿// <copyright file="ControlPlaneStampInfo.cs" company="Microsoft">
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
        private const int AzureStorageOrBatchAccountNameLengthMax = 24;
        private const int ShortUniquePrefixLengthMax = AzureStorageOrBatchAccountNameLengthMax - 10;
        private const string ComputeQueueKind = "cq";
        private const string StorageImageKind = "si";
        private const string VmAgentImageKind = "vm";
        private const string BatchAccountKind = "ba";
        private const string BillingStorageImageKind = "bl";

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlPlaneStampInfo"/> class.
        /// </summary>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="location">The control plane azure location.</param>
        /// <param name="stampShortUniquePrefix">A globally-unique prefix for storage/batch accounts.</param>
        /// <param name="controlPlaneStampSettings">The control plane stamp settings.</param>
        public ControlPlaneStampInfo(
            IControlPlaneInfo controlPlaneInfo,
            AzureLocation location,
            string stampShortUniquePrefix,
            ControlPlaneStampSettings controlPlaneStampSettings)
        {
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNullOrEmpty(stampShortUniquePrefix, nameof(stampShortUniquePrefix));
            Requires.Argument(stampShortUniquePrefix.Length <= ShortUniquePrefixLengthMax, nameof(stampShortUniquePrefix), $"The prefix '{nameof(stampShortUniquePrefix)}' must not be longer than {ShortUniquePrefixLengthMax} characters.");
            Requires.Argument(stampShortUniquePrefix.ToCharArray().All(c => char.IsLetterOrDigit(c)), nameof(stampShortUniquePrefix), $"The prefix '{nameof(stampShortUniquePrefix)}' contains invalid characters.");
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Location = location;
            AccountShortUniquePrefix = stampShortUniquePrefix;
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
        private static Dictionary<AzureLocation, string> RegionCodes { get; } = new Dictionary<AzureLocation, string>
        {
            { AzureLocation.EastUs, "use" },
            { AzureLocation.SouthEastAsia, "asse" },
            { AzureLocation.WestEurope, "euw" },
            { AzureLocation.WestUs2, "usw2" },
        };

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ControlPlaneStampSettings ControlPlaneStampSettings { get; }

        private string AccountShortUniquePrefix { get; }

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
        public string GetStampStorageAccountNameForBillingSubmission(AzureLocation billingLocation)
        {
            return MakeStorageAccountName(BillingStorageImageKind, billingLocation);
        }

        /// <inheritdoc/>
        public string GetStampStorageAccountNameForStorageImages(AzureLocation computeStorageLocation)
        {
            return MakeStorageAccountName(StorageImageKind, computeStorageLocation);
        }

        /// <inheritdoc/>
        public string GetStampBatchAccountName(AzureLocation azureLocation)
        {
            if (!DataPlaneLocations.Contains(azureLocation))
            {
                throw new NotSupportedException($"The data-plane location '{azureLocation}' is not supported in stamp '{this.StampResourceGroupName}'");
            }

            var regionCode = RegionCodes[azureLocation];
            var accountName = $"{AccountShortUniquePrefix}{ControlPlaneStampSettings.StampName}{BatchAccountKind}{regionCode}".ToLowerInvariant();
            if (accountName.Length > AzureStorageOrBatchAccountNameLengthMax)
            {
                throw new InvalidOperationException($"The resulting batch account name '{accountName}' must not be longer than {AzureStorageOrBatchAccountNameLengthMax} characters.");
            }

            return accountName;
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

            var regionCode = RegionCodes[azureLocation];
            var accountName = $"{AccountShortUniquePrefix}-{ControlPlaneStampSettings.StampName}-{kind}-{regionCode}".Replace("-", string.Empty).ToLowerInvariant();
            if (accountName.Length > AzureStorageOrBatchAccountNameLengthMax)
            {
                throw new InvalidOperationException($"The resulting storage account name '{accountName}' must not be longer than {AzureStorageOrBatchAccountNameLengthMax} characters.");
            }

            return accountName;
        }
    }
}
