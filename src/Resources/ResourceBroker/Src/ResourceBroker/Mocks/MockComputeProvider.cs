// <copyright file="MockComputeProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Mocks
{
    /// <summary>
    /// Mock compute provider.
    /// </summary>
    public class MockComputeProvider : BaseMockResourceProvider, IComputeProvider
    {
        /// <inheritdoc/>
        public async Task<VirtualMachineProviderStartComputeResult> StartComputeAsync(VirtualMachineProviderStartComputeInput input, IDiagnosticsLogger logger)
        {
            return await RunAsync<VirtualMachineProviderStartComputeInput, VirtualMachineProviderStartComputeResult>(input, logger);
        }

        /// <inheritdoc/>
        public async Task<VirtualMachineProviderCreateResult> CreateAsync(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger)
        {
            return await RunAsync<VirtualMachineProviderCreateInput, VirtualMachineProviderCreateResult>(input, logger);
        }

        /// <inheritdoc/>
        public async Task<VirtualMachineProviderDeleteResult> DeleteAsync(VirtualMachineProviderDeleteInput input, IDiagnosticsLogger logger)
        {
            return await RunAsync<VirtualMachineProviderDeleteInput, VirtualMachineProviderDeleteResult>(input, logger);
        }

        /// <inheritdoc/>
        public async Task<VirtualMachineProviderShutdownResult> ShutdownAsync(VirtualMachineProviderShutdownInput input, IDiagnosticsLogger logger)
        {
            return await RunAsync<VirtualMachineProviderShutdownInput, VirtualMachineProviderShutdownResult>(input, logger);
        }

        /// <inheritdoc/>
        public async Task<VirtualMachineProviderUpdateTagsResult> UpdateTagsAsync(VirtualMachineProviderUpdateTagsInput input, IDiagnosticsLogger logger)
        {
            return await RunAsync<VirtualMachineProviderUpdateTagsInput, VirtualMachineProviderUpdateTagsResult>(input, logger);
        }
    }
}
