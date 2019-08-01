// <copyright file="IResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using ResourceBroker.Models;

namespace ResourceBroker.Abstractions
{
    public interface IResourceBroker
    {
        // AllocateStorage
        Task<AllocateStorageResult> AllocateStorageAsync(AllocateStorageInput input, string continuationToken = null);

        // AllocateCompute
        Task<AllocateComputeResult> AllocateComputeAsync(AllocateComputeInput inout, string continuationToken = null);
    }
}