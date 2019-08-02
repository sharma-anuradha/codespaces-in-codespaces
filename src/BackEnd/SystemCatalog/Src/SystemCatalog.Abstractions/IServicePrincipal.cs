// <copyright file="IServicePrincipal.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions
{
    /// <summary>
    /// Represents an AAD service principal.
    /// </summary>
    public interface IServicePrincipal
    {
        /// <summary>
        /// Gets the service principal client id (aka appid).
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// Gets the service principal tenant id.
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// Gets the client secret for this service principal.
        /// </summary>
        /// <returns>A task whose result is the client secret.</returns>
        Task<string> GetServicePrincipalClientSecret();
    }
}
