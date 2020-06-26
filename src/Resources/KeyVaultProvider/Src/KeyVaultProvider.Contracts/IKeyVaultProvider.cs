// <copyright file="IKeyVaultProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider
{
    /// <summary>
    /// Key vault provider interface.
    /// </summary>
    public interface IKeyVaultProvider
    {
        /// <summary>
        /// Create a Key Vault resource.
        /// </summary>
        /// <param name="input">Provides input to Create Azure Key Vault.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>
        /// Result of the Create operations which includes continuationToken which
        /// can be used to call this method again to continue the next step of operation.
        /// </returns>
        Task<KeyVaultProviderCreateResult> CreateAsync(KeyVaultProviderCreateInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a Key Vault resource.
        /// </summary>
        /// <param name="input">Provides input to Delete Azure Key Vault.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>
        ///  Result of the Delete operations which includes continuationToken which
        ///  can be used to call this method again to continue the next step of operation.
        /// </returns>
        Task<KeyVaultProviderDeleteResult> DeleteAsync(KeyVaultProviderDeleteInput input, IDiagnosticsLogger logger);
    }
}
