// <copyright file="IAllocateResource.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions
{
    /// <summary>
    /// Interface which exposes allocating resources.
    /// </summary>
    public interface IAllocateResource
    {
        /// <summary>
        /// Allocate resource set based on input manifest.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="inputs">Target input manifest.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An <see cref="AllocateResult"/> enumerable object.</returns>
        Task<IEnumerable<AllocateResult>> AllocateAsync(
            Guid environmentId, IEnumerable<AllocateInput> inputs, string trigger, IDiagnosticsLogger logger);

        /// <summary>
        /// Allocate a compute or storage resource.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="input">The allocate input object.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>An <see cref="AllocateResult"/> object.</returns>
        Task<AllocateResult> AllocateAsync(
            Guid environmentId, AllocateInput input, string trigger, IDiagnosticsLogger logger);
    }
}
