// <copyright file="MockComputeProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Mocks
{
    /// <summary>
    /// 
    /// </summary>
    public class MockComputeProvider : IComputeProvider
    {
        /// <summary>
        /// 
        /// </summary>
        public MockComputeProvider()
        {
            Random = new Random();
        }

        private Random Random { get; }

        /// <inheritdoc/>
        public async Task<VirtualMachineProviderStartComputeResult> StartComputeAsync(VirtualMachineProviderStartComputeInput input, string continuationToken = null)
        {
            // TODO: More that needs to happen here but should be fine for the moment.

            await Task.Delay(Random.Next(100, 1000));

            return new VirtualMachineProviderStartComputeResult();
        }

        /// <inheritdoc/>
        public Task<VirtualMachineProviderCreateResult> CreateAsync(VirtualMachineProviderCreateInput input, string continuationToken = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<VirtualMachineProviderDeleteResult> DeleteAsync(VirtualMachineProviderDeleteInput input, string continuationToken = null)
        {
            throw new NotImplementedException();
        }
    }
}
