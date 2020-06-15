// <copyright file="ICreateVirtualMachineStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine.Strategies
{
    /// <summary>
    /// Virtual machine creation strategies.
    /// </summary>
    public interface ICreateVirtualMachineStrategy
    {
        /// <summary>
        /// Gets virtual machine template Json.
        /// </summary>
        string VirtualMachineTemplateJson { get; }

        /// <summary>
        /// Begin CreateVirtual Machine.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateVirtualMachine(
            VirtualMachineProviderCreateInput input,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Used by clients to determine which strategy to use for this input.
        /// </summary>
        /// <param name="input">input.</param>
        /// <returns>result.</returns>
        bool Accepts(VirtualMachineProviderCreateInput input);
    }
}