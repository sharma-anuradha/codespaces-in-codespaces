// <copyright file="ResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    public class ResourceBroker : IResourceBroker
    {
        public Task<AllocateComputeResult> AllocateComputeAsync(AllocateComputeInput inout, string continuationToken = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<AllocateStorageResult> AllocateStorageAsync(AllocateStorageInput input, string continuationToken = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
