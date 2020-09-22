using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Test
{
    public class MockControlPlaneAzureResourceAccessor : IControlPlaneAzureResourceAccessor
    {
        private IAzureClientFactory clientFactory;

        public MockControlPlaneAzureResourceAccessor(IAzureClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        public Task<string> GetCurrentSubscriptionIdAsync()
        {
            throw new NotImplementedException();
        }

        public Task<(string, string)> GetGlobalCosmosDbAccountAsync()
        {
            throw new NotImplementedException();
        }

        public Task<(string, string)> GetRegionalCosmosDbAccountAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<SecretItem>> GetKeyVaultSecretVersionsAsync(string secretName)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetKeyVaultSecretAsync(string secretName, string version)
        {
            throw new NotImplementedException();
        }

        public Task<(string, string)> GetStampCosmosDbAccountAsync()
        {
            throw new NotImplementedException();
        }

        public Task<(string, string)> GetStampStorageAccountAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<QueueStorageInfo> GetStampStorageAccountForComputeQueuesAsync(AzureLocation computeVmLocation, IDiagnosticsLogger logger = null)
        {
            const string QueueStorageAccount = "teststoragedevusw2";
            const string QueueResourceGroup = "testvmqueueresourcegroup";
            var subscriptionId = new Guid("86642df6-843e-4610-a956-fdd497102261");
            var azure = await clientFactory.GetAzureClientAsync(subscriptionId);
            await azure.CreateResourceGroupIfNotExistsAsync(QueueResourceGroup, computeVmLocation.ToString());
            var storageAccount = await azure.CreateStorageAccountIfNotExistsAsync(
                QueueResourceGroup,
                computeVmLocation.ToString(),
                QueueStorageAccount);
            if (storageAccount == null)
            {
                // storage account does not exist
                throw new ArgumentException($"Could not find storage account {QueueStorageAccount} in resource group {QueueResourceGroup}");
            }

            var keys = await storageAccount.GetKeysAsync();
            var storageAccountKey = (keys.Count > 1) ? keys[0].Value : default;
            if (storageAccountKey == default)
            {
                throw new Exception($"Could not find storage account key for storage account : {QueueStorageAccount}");
            }

            return new QueueStorageInfo()
            {
                ResourceGroup = QueueResourceGroup,
                StorageAccountKey = storageAccountKey,
                StorageAccountName = storageAccount.Name,
                SubscriptionId = subscriptionId.ToString(),
            };
        }

        public Task<(string, string)> GetStampStorageAccountForComputeVmAgentImagesAsync(AzureLocation computeVmLocation)
        {
            throw new NotImplementedException();
        }

        public Task<(string, string)> GetStampStorageAccountForStorageImagesAsync(AzureLocation computeStorageLocation)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public List<string> GetStampOrigins()
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

        /// <inheritdoc/>
        public Task<(string, string)> GetStampStorageAccountForPartner(AzureLocation billingSubmissionLocation, string partnerId, string partnerName)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetStampServiceBusConnectionStringAsync(IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        public Task<(string, string, string)> GetApplicationKeyAndSecretsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<(string, string)> GetStampStorageAccountForPartner(AzureLocation billingSubmissionLocation, string partnerId)
        {
            throw new NotImplementedException();
        }

        public Task<AzureCredentials> GetAzureCredentialsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<(string, string)>> GetAllStampStorageAccountForPartner(string partnerId)
        {
            throw new NotImplementedException();
        }

        public Task<QueueStorageInfo> GetStampStorageAccountForPoolQueuesAsync(IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
