// <copyright file="ResourcePoolDimensionsKeys.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    /// Constants for resource pool dimensions.
    /// </summary>
    public class ResourcePoolDimensionsKeys
    {
        /// <summary>
        /// Compute OS key.
        /// </summary>
        public const string ComputeOS = "computeOS";

        /// <summary>
        /// Location key.
        /// </summary>
        public const string Location = "location";

        /// <summary>
        /// Image name key.
        /// </summary>
        public const string ImageName = "imageName";

        /// <summary>
        /// Image family name key.
        /// </summary>
        public const string ImageFamilyName = "imageFamilyName";

        /// <summary>
        /// Size In GB key.
        /// </summary>
        public const string SizeInGB = "sizeInGB";

        /// <summary>
        /// Sku name key.
        /// </summary>
        public const string SkuName = "skuName";
    }
}
