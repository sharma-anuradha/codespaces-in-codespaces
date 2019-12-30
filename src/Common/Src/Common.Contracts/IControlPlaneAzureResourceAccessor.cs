// <copyright file="IControlPlaneAzureResourceAccessor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Provides access to various control-plane azure resources.
    /// </summary>
    public interface IControlPlaneAzureResourceAccessor
    {
        /// <summary>
        /// Gets the current azure subscription id.
        /// </summary>
        /// <returns>The subscription id.</returns>
        Task<string> GetCurrentSubscriptionIdAsync();

        /// <summary>
        /// Gets a secret from the environment key vault.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="version">Version of the secret. Current if not specified.</param>
        /// <returns>The secret value.</returns>
        Task<string> GetKeyVaultSecretAsync(string secretName, string version);

        /// <summary>
        /// Gets the versions of the secret in the environment key vault.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <returns>Enumerable versions of the secret.</returns>
        Task<IEnumerable<SecretItem>> GetKeyVaultSecretVersionsAsync(string secretName);

        /// <summary>
        /// Gets the DNS origins from the control panel info.
        /// </summary>
        /// <returns>DNS origins.</returns>
        List<string> GetStampOrigins();

        /// <summary>
        /// Gets the instance-level cosmos db account.
        /// </summary>
        /// <returns>A tuple of the account url and the account key.</returns>
        Task<(string, string)> GetInstanceCosmosDbAccountAsync();

        /// <summary>
        /// Gets the stamp-level cosmos db account.
        /// </summary>
        /// <returns>A tuple of the account url and the account key.</returns>
        Task<(string, string)> GetStampCosmosDbAccountAsync();

        /// <summary>
        /// Gets the stamp-level storage account.
        /// </summary>
        /// <returns>A tuple of the account name and the account key.</returns>
        Task<(string, string)> GetStampStorageAccountAsync();

        /// <summary>
        /// Gets the stamp-level storage account used for compute job queues.
        /// </summary>
        /// <param name="computeVmLocation">The azure location of the compute vm.</param>
        /// <param name="logger">The logger instance.</param>
        /// <returns>A tuple of the account name and the account key.</returns>
        Task<(string, string)> GetStampStorageAccountForComputeQueuesAsync(AzureLocation computeVmLocation, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the stamp-level storage account used for compute vm agent images..
        /// </summary>
        /// <param name="computeVmLocation">The azure location of the compute vm.</param>
        /// <returns>A tuple of the account name and the account key.</returns>
        Task<(string, string)> GetStampStorageAccountForComputeVmAgentImagesAsync(AzureLocation computeVmLocation);

        /// <summary>
        /// Gets the stamp-level storage account used for compute storage images.
        /// </summary>
        /// <param name="computeStorageLocation">The azure location of the compute storage.</param>
        /// <returns>A tuple of the account name and the account key.</returns>
        Task<(string, string)> GetStampStorageAccountForStorageImagesAsync(AzureLocation computeStorageLocation);

        /// <summary>
        /// Gets the stamp-level batch account.
        /// </summary>
        /// <param name="location">The data-plane azure location.</param>
        /// <param name="logger">The logger instance.</param>
        /// <returns>A tuple of the account name, account key, account endpoint.</returns>
        Task<(string, string, string)> GetStampBatchAccountAsync(AzureLocation location, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the stamp-level storage account used for billing submission.
        /// </summary>
        /// <param name="billingSubmissionLocation">The azure location of the billing submission data.</param>
        /// <returns>A tuple of the account name and the account key.</returns>
        Task<(string, string)> GetStampStorageAccountForBillingSubmission(AzureLocation billingSubmissionLocation);
    }
}
