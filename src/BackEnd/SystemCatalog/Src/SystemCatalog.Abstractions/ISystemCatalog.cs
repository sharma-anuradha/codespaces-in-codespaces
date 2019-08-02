// <copyright file="ISystemCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions
{
    /// <summary>
    /// Represents the BackEnd system catalog.
    /// </summary>
    public interface ISystemCatalog
    {
        /// <summary>
        /// Gets the Azure subscription catalog, for all Azure subscriptons known to the BackEnd.
        /// </summary>
        IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        /// <summary>
        /// Gets the SKU catalog for the Cloud Environment SKUs known to the BackEnd.
        /// </summary>
        ISkuCatalog SkuCatalog { get; }
    }
}
