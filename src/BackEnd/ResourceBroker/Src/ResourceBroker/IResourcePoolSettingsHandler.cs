// <copyright file="IResourcePoolManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Handler which accepts resource pool settings updates.
    /// </summary>
    public interface IResourcePoolSettingsHandler
    {
        /// <summary>
        /// Allows .
        /// </summary>
        /// <param name="enabledState">Target enabled state.</param>
        /// <returns>Scaling result.</returns>
        Task UpdateResourceEnabledStateAsync(IDictionary<string, bool> enabledState);
    }
}
