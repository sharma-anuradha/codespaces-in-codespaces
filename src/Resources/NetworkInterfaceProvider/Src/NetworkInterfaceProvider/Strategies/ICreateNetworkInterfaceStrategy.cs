// <copyright file="ICreateNetworkInterfaceStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Strategies
{
    /// <summary>
    /// Network interface creation strategies.
    /// </summary>
    public interface ICreateNetworkInterfaceStrategy
    {
        /// <summary>
        /// Gets virtual machine template Json.
        /// </summary>
        string NetworkInterfaceTemplateJson { get; }

        /// <summary>
        /// Begin Create Network Interface.
        /// </summary>
        /// <param name="input">input.</param>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateNetworkInterface(
            NetworkInterfaceProviderCreateInput input,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Used by clients to determine which strategy to use for this input.
        /// </summary>
        /// <param name="input">input.</param>
        /// <returns>result.</returns>
        bool Accepts(NetworkInterfaceProviderCreateInput input);
    }
}
