// <copyright file="IResourcePoolDefinitionStore.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Store that manages the resource pool definitions.
    /// </summary>
    public interface IResourcePoolDefinitionStore
    {
        /// <summary>
        /// Return latest scaling levels that has been reported by the Scaling Engine.
        /// </summary>
        /// <returns>Latest scaling levels list.</returns>
        Task<IList<ResourcePool>> RetrieveDefinitionsAsync();
    }
}
