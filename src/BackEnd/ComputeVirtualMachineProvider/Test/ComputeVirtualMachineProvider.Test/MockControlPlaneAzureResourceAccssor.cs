﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Rest.Azure;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Test
{
    public class MockControlPlaneAzureResourceAccssor : IControlPlaneAzureResourceAccessor
    {
        private IAzureClientFactory clientFactory;

        public MockControlPlaneAzureResourceAccssor(IAzureClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        public Task<string> GetCurrentSubscriptionIdAsync()
        {
            throw new NotImplementedException();
        }

        public Task<(string, string)> GetInstanceCosmosDbAccountAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IPage<SecretItem>> GetKeyValutSecretVersionsAsync(string secretName)
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

        public async Task<(string, string)> GetStampStorageAccountForComputeQueuesAsync(AzureLocation computeVmLocation)
        {
            const string QueueStorageAccount = "teststoragedevusw2";
            const string QueueResourceGroup = "testvmqueueresourcegroup";
            var azure = await clientFactory.GetAzureClientAsync(new Guid("86642df6-843e-4610-a956-fdd497102261"));
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

            return (storageAccount.Name, storageAccountKey);

        }

        public Task<(string, string)> GetStampStorageAccountForComputeVmAgentImagesAsync(AzureLocation computeVmLocation)
        {
            throw new NotImplementedException();
        }

        public Task<(string, string)> GetStampStorageAccountForStorageImagesAsync(AzureLocation computeStorageLocation)
        {
            throw new NotImplementedException();
        }
    }
}
