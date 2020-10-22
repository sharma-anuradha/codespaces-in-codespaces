// <copyright file="IEnvironmentPoolDefinitionStore.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Jobs
{
    public interface IEnvironmentPoolDefinitionStore
    {
        /// <summary>
        /// Return latest target levels for pool..
        /// </summary>
        /// <returns>Latest target levels list.</returns>
        Task<IList<EnvironmentPool>> RetrieveDefinitionsAsync();
    }
}