// <copyright file="IQuotaFamilyCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Stores information about Quota Families.
    /// </summary>
    public interface IQuotaFamilyCatalog
    {
        /// <summary>
        /// Gets the quota families.
        /// </summary>
        IReadOnlyDictionary<string, IDictionary<string, int>> QuotaFamilies { get; }
    }
}
