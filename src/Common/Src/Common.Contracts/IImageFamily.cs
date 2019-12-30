// <copyright file="IImageFamily.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Represents an instance of an image family.
    /// </summary>
    public interface IImageFamily
    {
        /// <summary>
        /// Gets the image family name.
        /// </summary>
        string ImageFamilyName { get; }
    }
}
