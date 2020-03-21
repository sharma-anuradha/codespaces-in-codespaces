// <copyright file="ResourceRecordExtension.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Resource Record Extension.
    /// </summary>
    public static class ResourceRecordExtension
    {
        /// <summary>
        /// Builds wrapping compute details object to proxy through to
        /// underlying backing properties.
        /// </summary>
        /// <param name="record">Target record.</param>
        /// <returns>Wrapping object.</returns>
        public static ResourceProxyDetails GetDetails(this ResourceRecord record)
        {
            return new ResourceProxyDetails(record);
        }

        /// <summary>
        /// Builds wrapping compute details object to proxy through to
        /// underlying backing properties.
        /// </summary>
        /// <param name="record">Target record.</param>
        /// <returns>Wrapping object.</returns>
        public static ResourceProxyStorageDetails GetStorageDetails(this ResourceRecord record)
        {
            if (record.Type == Common.Contracts.ResourceType.StorageFileShare
                || record.Type == Common.Contracts.ResourceType.StorageArchive)
            {
                return new ResourceProxyStorageDetails(record);
            }

            throw new NotSupportedException($"Current type of this resource `{record.Type}` is not a storage type.");
        }

        /// <summary>
        /// Builds wrapping compute details object to proxy through to
        /// underlying backing properties.
        /// </summary>
        /// <param name="record">Target record.</param>
        /// <returns>Wrapping object.</returns>
        public static ResourceProxyComputeDetails GetComputeDetails(this ResourceRecord record)
        {
            if (record.Type == Common.Contracts.ResourceType.ComputeVM)
            {
                return new ResourceProxyComputeDetails(record);
            }

            throw new NotSupportedException($"Current type of this resource `{record.Type}` is not a compute type.");
        }
    }
}
