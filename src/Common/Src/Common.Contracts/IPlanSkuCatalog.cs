// <copyright file="IPlanSkuCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Plan sku catalog.
    /// </summary>
    public interface IPlanSkuCatalog
    {
        /// <summary>
        /// Gets plan skus.
        /// </summary>
        IReadOnlyDictionary<string, IPlanSku> PlanSkus { get; }

        /// <summary>
        /// Gets the default plan sku name.
        /// </summary>
        string DefaultSkuName { get; }
    }
}
