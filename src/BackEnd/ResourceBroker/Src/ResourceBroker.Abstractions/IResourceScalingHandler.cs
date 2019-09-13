// <copyright file="IResourceScalingHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions
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
        /// <returns>Scaling result.</returns>
        Task<ScalingResult> UpdateResourceScaleLevels(ScalingInput scalingInput);
    }
}
