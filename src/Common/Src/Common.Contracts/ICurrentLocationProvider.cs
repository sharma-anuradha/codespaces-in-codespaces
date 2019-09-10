// <copyright file="ICurrentLocationProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Provides the current azure location.
    /// </summary>
    public interface ICurrentLocationProvider
    {
        /// <summary>
        /// Gets the current azure location.
        /// </summary>
        AzureLocation CurrentLocation { get; }
    }
}