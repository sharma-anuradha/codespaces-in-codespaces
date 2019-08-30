// <copyright file="IResourceScalingBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions
{
    /// <summary>
    ///
    /// </summary>
    public interface IResourceScalingBroker
    {
        /// <summary>
        /// Allows the Scaling Engine to update the current scaling levels.
        /// </summary>
        /// <param name="resourceScaleLevels"></param>
        /// <returns></returns>
        Task<ScalingResult> UpdateResourceScaleLevels(IEnumerable<ScalingInput> scalingInputs);
    }
}
