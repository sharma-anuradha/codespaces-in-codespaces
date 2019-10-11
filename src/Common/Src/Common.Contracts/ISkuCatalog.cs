// <copyright file="ISkuCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// The Cloud Environment SKU catalog.
    /// </summary>
    public interface ISkuCatalog
    {
        /// <summary>
        /// Gets the list of Cloud Environment SKUs.
        /// </summary>
        IReadOnlyDictionary<string, ICloudEnvironmentSku> CloudEnvironmentSkus { get; }

        /// <summary>
        /// Gets the VM Image Families.
        /// </summary>
        IReadOnlyDictionary<string, IBuildArtifactImageFamily> BuildArtifactImageFamilies { get; }
    }
}
