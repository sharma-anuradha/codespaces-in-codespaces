// <copyright file="ICrossRegionControlPlaneInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Provides azure resource accessors, location providers, and control plane information for all control plane regions.
    /// </summary>
    public interface ICrossRegionControlPlaneInfo
    {
        /// <summary>
        /// Gets all control plane azure resource accessor objects for all control plane regions.
        /// </summary>
        IReadOnlyDictionary<AzureLocation, IControlPlaneAzureResourceAccessor> AllResourceAccessors { get; }

        /// <summary>
        /// Gets all location provider objects for all control plane regions.
        /// </summary>
        IReadOnlyDictionary<AzureLocation, ICurrentLocationProvider> AllLocationProviders { get; }

        /// <summary>
        /// Gets all control plane info objects for all control plane regions.
        /// </summary>
        IReadOnlyDictionary<AzureLocation, IControlPlaneInfo> AllControlPlaneInfos { get; }
    }
}
