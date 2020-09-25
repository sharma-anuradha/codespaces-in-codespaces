// <copyright file="IInitScriptUrlGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Generates init script url.
    /// </summary>
    public interface IInitScriptUrlGenerator
    {
        /// <summary>
        /// Generates a SAS signed read-only URL for init script.
        /// </summary>
        /// <param name="location">Azure location.</param>
        /// <param name="logger">Diagnostics logger.</param>
        /// <returns>Url.</returns>
        Task<string> GetInitScriptUrlAsync(AzureLocation location, IDiagnosticsLogger logger);
    }
}
