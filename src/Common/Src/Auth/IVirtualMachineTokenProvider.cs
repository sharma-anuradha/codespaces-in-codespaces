// <copyright file="IVirtualMachineTokenProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions
{
    /// <summary>
    /// Provides methods to create security tokens.
    /// </summary>
    public interface IVirtualMachineTokenProvider
    {
        /// <summary>
        /// Generates a token.
        /// </summary>
        /// <param name="identifier">Id of the resource.</param>
        /// <param name="logger">logger.</param>
        /// <returns>security token.</returns>
        Task<string> GenerateAsync(string identifier, IDiagnosticsLogger logger);
    }
}