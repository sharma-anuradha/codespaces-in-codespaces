// <copyright file="IResourceScalingHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    /// Handler which accepts scaling requests.
    /// </summary>
    public interface IResourceScalingHandler
    {
        /// <summary>
        /// Allows the Scaling Engine to update the current scaling levels.
        /// </summary>
        /// <param name="scalingInput">Target scaling input.</param>
        /// <returns>A task representing completion of the update operation.</returns>
        Task UpdateResourceScaleLevels(ScalingInput scalingInput);
    }
}
