// <copyright file="IResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
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
        /// <param name="input"></param>
        /// <param name="continuationToken"></param>
        /// <returns></returns>
        Task<AllocateStorageResult> AllocateStorageAsync(AllocateStorageInput input, string continuationToken = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inout"></param>
        /// <param name="continuationToken"></param>
        /// <returns></returns>
        Task<AllocateComputeResult> AllocateComputeAsync(AllocateComputeInput inout, string continuationToken = null);


        // number of items in the pool at any one time and unassigned

        // partion = region and resource type
    }
}