// <copyright file="ICodespacesApiClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.CodespacesApiClient
{
    /// <summary>
    /// Codespaces FrontEnd Client.
    /// </summary>
    public interface ICodespacesApiClient
    {
        /// <summary>
        /// Fetches environment record from FrontEnd service.
        /// </summary>
        /// <param name="codespaceId">The codespace id.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<CloudEnvironmentResult> GetCodespaceAsync(string codespaceId, IDiagnosticsLogger logger);
    }
}