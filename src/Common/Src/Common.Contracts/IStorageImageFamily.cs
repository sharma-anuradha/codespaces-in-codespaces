// <copyright file="IVmImageFamily.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Represents an instance of an image family.
    /// </summary>
    public interface IStorageImageFamily : IImageFamily
    {
        /// <summary>
        /// Gets the image name for the storage image.
        /// </summary>
        string ImageName { get; }
    }
}
