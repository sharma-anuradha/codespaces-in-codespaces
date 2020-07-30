// <copyright file="IFirstPartyTokenBuilder.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// A helper to build first party AAD access tokens.
    /// </summary>
    public interface IFirstPartyTokenBuilder
    {
        /// <summary>
        /// Builds an FPA token, using the first party tenant ID.
        /// </summary>
        /// <param name="logger">A logger instance.</param>
        /// <returns>An access token.</returns>
        Task<AuthenticationResult> GetFpaTokenAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Builds an FPA token, with the specified tenant ID.
        /// </summary>
        /// <param name="tenantId">The tenant ID used in the builder.</param>
        /// <param name="logger">A logger instance.</param>
        /// <returns>An access token.</returns>
        Task<AuthenticationResult> GetFpaTokenAsync(string tenantId, IDiagnosticsLogger logger);

        /// <summary>
        /// Builds an resource-scoped token using the MSI first party app.
        /// </summary>
        /// <param name="authorityUri">The authority returned from MSI (ex. https://login.microsoftonline.com/72F988BF-86F1-41AF-91AB-2D7CD011DB47 ).</param>
        /// <param name="resource">The resource returned from MSI (ex. https://serviceidentity.azure.net/ ).</param>
        /// <param name="logger">A logger instance.</param>
        /// <returns>An access token.</returns>
        Task<AuthenticationResult> GetMsiResourceTokenAsync(string authorityUri, string resource, IDiagnosticsLogger logger);
    }
}