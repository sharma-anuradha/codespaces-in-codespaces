// <copyright file="MockControlPlaneAzureResourceAccessor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Mock control plane accessor.
    /// </summary>
    public class MockControlPlaneAzureResourceAccessor : IControlPlaneAzureResourceAccessor
    {
        private const string Version1 = "version1";
        private const string Version2 = "version2";

        private Dictionary<string, string> versionToSecretMap = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MockControlPlaneAzureResourceAccessor"/> class.
        /// </summary>
        /// <param name="certificateSettings">certificate settings.</param>
        public MockControlPlaneAzureResourceAccessor(CertificateSettings certificateSettings)
        {
            versionToSecretMap[Version2] = certificateSettings.MockPrimaryCertificate;
            versionToSecretMap[Version1] = certificateSettings.MockSecondaryCertificate;
        }

        /// <inheritdoc/>
        public Task<string> GetCurrentSubscriptionIdAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public List<string> GetStampOrigins()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<(string, string)> GetInstanceCosmosDbAccountAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SecretItem>> GetKeyVaultSecretVersionsAsync(string secretName)
        {
            await Task.CompletedTask;

            return new SecretItem[]
            {
                new SecretItem(
                    new SecretIdentifier("https://mykeyvault/secret", "certname", Version1).ToString(),
                    new SecretAttributes(true, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddYears(1), DateTime.UtcNow.AddDays(-2), null, null),
                    managed: true),
                new SecretItem(
                    new SecretIdentifier("https://mykeyvault/secret", "certname", Version2).ToString(),
                    new SecretAttributes(true, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddYears(1), DateTime.UtcNow.AddDays(-2), null, null),
                    managed: true),
            };
        }

        /// <inheritdoc/>
        public async Task<string> GetKeyVaultSecretAsync(string secretName, string version = null)
        {
            await Task.CompletedTask;
            if (string.IsNullOrWhiteSpace(version))
            {
                return versionToSecretMap[Version2];
            }

            return versionToSecretMap[version];
        }

        /// <inheritdoc/>
        public Task<(string, string)> GetStampCosmosDbAccountAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<(string, string)> GetStampStorageAccountAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<(string, string)> GetStampStorageAccountForComputeQueuesAsync(AzureLocation computeVmLocation, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<(string, string)> GetStampStorageAccountForComputeVmAgentImagesAsync(AzureLocation computeVmLocation)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<(string, string)> GetStampStorageAccountForStorageImagesAsync(AzureLocation computeStorageLocation)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<(string, string, string)> GetStampBatchAccountAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<(string, string)> GetStampStorageAccountForBillingSubmission(AzureLocation billingSubmissionLocation)
        {
            throw new NotImplementedException();
        }
    }
}
