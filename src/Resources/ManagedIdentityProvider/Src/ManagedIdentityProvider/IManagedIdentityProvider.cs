// <copyright file="IManagedIdentityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// Provides access to the Azure Managed Identity Resource Provider.
    /// </summary>
    public interface IManagedIdentityProvider
    {
        /// <summary>
        /// A GET operation to retrieve system assigned credentials for a given resource.
        /// </summary>
        /// <param name="identityUrl">The system-assigned identity URL provided by ARM.</param>
        /// <returns>System assigned credentials for a resource.</returns>
        Task<CredentialResponse> GetSystemAssignedCredentialsAsync(string identityUrl, IDiagnosticsLogger logger);
    }
}
