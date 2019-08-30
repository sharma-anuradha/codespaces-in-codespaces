// <copyright file="IResourceScalingStore.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    ///
    /// </summary>
    public interface IResourceScalingStore
    {
        /// <summary>
        /// Return latest scaling levels that has been reported by the Scaling Engine.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<ResourcePoolDefinition>> RetrieveLatestScaleLevels();
    }
}
