// <copyright file="ResourceProxyStorageDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Resource Storage Details.
    /// </summary>
    public class ResourceProxyStorageDetails : ResourceProxyDetails
    {
        private const string SizeInGBName = "sizeInGB";
        private const string SourceComputeOSName = "sourceComputeOS";
        private const string ArchiveStorageStrategyName = "archiveStorageStrategy";
        private const string ArchiveStorageBlobNameName = "archiveStorageBlobName";
        private const string ArchiveStorageBlobContainerNameName = "archiveStorageBlobContainerName";
        private const string ArchiveStorageSourceSizeInGbName = "archiveStorageSourceSizeInGb";
        private const string ArchiveStorageBlobStoredSizeInGbName = "archiveStorageBlobStoredSizeInGb";
        private const string ArchiveStorageSourceResourceIdName = "archiveStorageSourceResourceId";
        private const string ArchiveStorageSourceStorageAccountNameName = "archiveStorageSourceStorageAccountName";
        private const string ArchiveStorageSourceSkuNameName = "archiveStorageSourceSkuName";
        private const string ArchiveStorageSourceFileNameName = "archiveStorageSourceFileName";
        private const string ArchiveStorageSourceFileShareNameName = "archiveStorageSourceFileShareName";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceProxyStorageDetails"/> class.
        /// </summary>
        /// <param name="record">Target record.</param>
        public ResourceProxyStorageDetails(ResourceRecord record)
            : base(record)
        {
        }

        /// <summary>
        /// Gets the underling image name.
        /// </summary>
        public int SizeInGB
        {
            get
            {
                var rawValue = Record?.PoolReference?.Dimensions.GetValueOrDefault(SizeInGBName)
                    ?? Record.Properties.GetValueOrDefault(SizeInGBName)
                    ?? "0";
                return int.Parse(rawValue);
            }
        }

        /// <summary>
        /// Gets or Sets which strategy was used for archiving the resource.
        /// </summary>
        public ResourceArchiveStrategy? ArchiveStorageStrategy
        {
            get
            {
                var rawValue = Record.Properties.GetValueOrDefault(ArchiveStorageStrategyName);
                return string.IsNullOrEmpty(rawValue) ? default(ResourceArchiveStrategy?) : (ResourceArchiveStrategy)Enum.Parse(typeof(ResourceArchiveStrategy), rawValue, true);
            }

            set
            {
                Record.Properties[ArchiveStorageStrategyName] = value.ToString();
            }
        }

        /// <summary>
        /// Gets or Sets the target blob name.
        /// </summary>
        public string ArchiveStorageBlobName
        {
            get { return Record.Properties.GetValueOrDefault(ArchiveStorageBlobNameName); }
            set { Record.Properties[ArchiveStorageBlobNameName] = value; }
        }

        /// <summary>
        /// Gets or Sets the target blob container name.
        /// </summary>
        public string ArchiveStorageBlobContainerName
        {
            get { return Record.Properties.GetValueOrDefault(ArchiveStorageBlobContainerNameName); }
            set { Record.Properties[ArchiveStorageBlobContainerNameName] = value; }
        }

        /// <summary>
        /// Gets or Sets the target size of the share in gb.
        /// </summary>
        public int? ArchiveStorageSourceSizeInGb
        {
            get
            {
                var rawValue = Record.Properties.GetValueOrDefault(ArchiveStorageSourceSizeInGbName);
                return string.IsNullOrEmpty(rawValue) ? default(int?) : int.Parse(rawValue);
            }

            set
            {
                Record.Properties[ArchiveStorageSourceSizeInGbName] = value.ToString();
            }
        }

        /// <summary>
        /// Gets or Sets the stored size of the share in gb.
        /// </summary>
        public double? ArchiveStorageBlobStoredSizeInGb
        {
            get
            {
                var rawValue = Record.Properties.GetValueOrDefault(ArchiveStorageBlobStoredSizeInGbName);
                return string.IsNullOrEmpty(rawValue) ? default(double?) : double.Parse(rawValue);
            }

            set
            {
                Record.Properties[ArchiveStorageBlobStoredSizeInGbName] = value.ToString();
            }
        }

        /// <summary>
        /// Gets or Sets the source resource id.
        /// </summary>
        public string ArchiveStorageSourceResourceId
        {
            get { return Record.Properties.GetValueOrDefault(ArchiveStorageSourceResourceIdName); }
            set { Record.Properties[ArchiveStorageSourceResourceIdName] = value; }
        }

        /// <summary>
        /// Gets or Sets the source storage account name.
        /// </summary>
        public string ArchiveStorageSourceStorageAccountName
        {
            get { return Record.Properties.GetValueOrDefault(ArchiveStorageSourceStorageAccountNameName); }
            set { Record.Properties[ArchiveStorageSourceStorageAccountNameName] = value; }
        }

        /// <summary>
        /// Gets or Sets the source storage sku.
        /// </summary>
        public string ArchiveStorageSourceSkuName
        {
            get { return Record.Properties.GetValueOrDefault(ArchiveStorageSourceSkuNameName); }
            set { Record.Properties[ArchiveStorageSourceSkuNameName] = value; }
        }

        /// <summary>
        /// Gets or Sets the source file name.
        /// </summary>
        public string ArchiveStorageSourceFileName
        {
            get { return Record.Properties.GetValueOrDefault(ArchiveStorageSourceFileNameName); }
            set { Record.Properties[ArchiveStorageSourceFileNameName] = value; }
        }

        /// <summary>
        /// Gets or Sets the source file share name.
        /// </summary>
        public string ArchiveStorageSourceFileShareName
        {
            get { return Record.Properties.GetValueOrDefault(ArchiveStorageSourceFileShareNameName); }
            set { Record.Properties[ArchiveStorageSourceFileShareNameName] = value; }
        }

        /// <summary>
        /// Gets or Sets the source compute os.
        /// </summary>
        public ComputeOS? SourceComputeOS
        {
            get
            {
                var rawValue = Record.Properties.GetValueOrDefault(SourceComputeOSName);
                return string.IsNullOrEmpty(rawValue) ? default(ComputeOS?) : (ComputeOS)Enum.Parse(typeof(ComputeOS), rawValue, true);
            }

            set
            {
                Record.Properties[SourceComputeOSName] = value.ToString();
            }
        }
    }
}
