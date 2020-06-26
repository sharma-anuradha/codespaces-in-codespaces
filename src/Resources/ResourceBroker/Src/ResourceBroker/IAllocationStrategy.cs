// <copyright file="IAllocationStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Resource allocation strategy.
    /// </summary>
    public interface IAllocationStrategy : IAllocateResource
    {
        /// <summary>
        /// Call to determine if this allocation strategy can handle this resource request.
        /// </summary>
        /// <param name="inputs">Allocation input requests.</param>
        /// <returns>True if it can handle the allocation inputs.</returns>
        bool CanHandle(IEnumerable<AllocateInput> inputs);
    }
}
