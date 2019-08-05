// <copyright file="IResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions
{
    /// <summary>
    ///
    /// </summary>
    public interface IResourceBroker
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="inout"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        Task<AllocateResult> AllocateAsync(AllocateInput input, IDiagnosticsLogger logger);
    }
}