// <copyright file="AzureVmProviderTestsBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Test
{
    public class AzureComputeProviderTestsBase : IDisposable
    {
        public IConfiguration Config { get; }
        public Guid SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public AzureLocation Location { get; }
        public string AuthFilePath { get; }
        public ISystemCatalog SystemCatalog { get; }
        public IServicePrincipal ServicePrincipal { get; }
        public IAzureClientFactory AzureClientFactory { get; }
        public IAzure Azure { get; }
        public IControlPlaneAzureResourceAccessor ResourceAccessor { get; }

        public AzureComputeProviderTestsBase()
        {
            Config = InitConfiguration();
            SubscriptionId = Guid.Parse(azureSubscriptionId);
            Location = azureLocation;
            ResourceGroupName = GetResourceGroupName();
            ServicePrincipal = GetServicePrincipal();
            SystemCatalog = GetMockSystemCatalog(ServicePrincipal).Object;
            AzureClientFactory = new AzureClientFactory(SystemCatalog.AzureSubscriptionCatalog);
            Azure = AzureClientFactory.GetAzureClientAsync(new Guid(azureSubscriptionId)).Result;
            ResourceAccessor = GetMockControlPlaneAzureResourceAccessor(Azure).Object;
        }

        private const string ComputeQueueResourceGroup = "vsclk-online-dev-ci-usw2";
        private const string ComputeQueueStorageAccount = "vsodevciusw2cqusw2";
        private const string ComputeVsoAgentImageStorageAccount = "vsodevciusw2vmusw2";
        private const string ComputeVsoAgentImageContainerName = "vsoagent";
        // Azure subscription to be used for tests
        private const string azureSubscriptionId = "86642df6-843e-4610-a956-fdd497102261";
        private const string azureSubscriptionName = "ignorethis";
        // Prefix for Azure resource group that will be created (then cleaned-up) by the test
        private const string azureResourceGroupPrefix = "test-vm-";
        private static readonly AzureLocation azureLocation = AzureLocation.WestUs2;

        private static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
            return config;
        }

        private static string GetResourceGroupName()
        {
            return azureResourceGroupPrefix + Environment.UserName;
        }

        private static IServicePrincipal GetServicePrincipal()
        {
            var config = InitConfiguration();

            var clientId = config["CLIENT_ID"];
            var tenantId = config["TENANT_ID"];
            var clientSecret = config["CLIENT_SECRET"];
            var secretProviderMoq = new Mock<ISecretProvider>();
            secretProviderMoq
                .Setup(p => p.GetSecretAsync(It.IsAny<string>()))
                .ReturnsAsync(clientSecret);

            return new ServicePrincipal(clientId, "ignorethis", tenantId, secretProviderMoq.Object);
        }

        private static Mock<ISystemCatalog> GetMockSystemCatalog(IServicePrincipal servicePrincipal)
        {
            var catalogMoq = new Mock<ISystemCatalog>();
            catalogMoq
                .Setup(x => x.AzureSubscriptionCatalog.AzureSubscriptions)
                .Returns(new[] {
                    new AzureSubscription(
                       azureSubscriptionId,
                        azureSubscriptionName,
                        servicePrincipal,
                        true,
                        new[] {azureLocation},
                        null,
                        null,
                        null,
                        null)
                });
            return catalogMoq;
        }

        private static Mock<IControlPlaneAzureResourceAccessor> GetMockControlPlaneAzureResourceAccessor(IAzure azure)
        {
            var resourceAccessor = new Mock<IControlPlaneAzureResourceAccessor>();
            resourceAccessor.Setup(x => x.GetStampStorageAccountForComputeQueuesAsync(It.IsAny<AzureLocation>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(async () => {
                    var sa = await azure.StorageAccounts.GetByResourceGroupAsync(ComputeQueueResourceGroup, ComputeQueueStorageAccount);
                    var saKey = sa.GetKeys().First();
                    var result = new ComputeQueueStorageInfo()
                    {
                        ResourceGroup = ComputeQueueResourceGroup,
                        StorageAccountKey = saKey.Value,
                        StorageAccountName = sa.Name,
                        SubscriptionId = azureSubscriptionId,
                    };
                    return result;
                });
            return resourceAccessor;
        }

        public async Task<string> GetSrcBlobUrlAsync(string vsoBlobName)
        {
            // Get storage account key
            var storageAccountsInSubscription = await Azure.StorageAccounts.ListAsync();
            var srcStorageAccount = storageAccountsInSubscription.Single(sa => sa.Name == ComputeVsoAgentImageStorageAccount);
            var srcStorageAccountKey = (await srcStorageAccount.GetKeysAsync())[0].Value;

            // Create blob sas url
            var storageCreds = new StorageCredentials(srcStorageAccount.Name, srcStorageAccountKey);
            var blobRef = new CloudStorageAccount(storageCreds, useHttps: true)
                .CreateCloudBlobClient()
                .GetContainerReference(ComputeVsoAgentImageContainerName)
                .GetBlobReference(vsoBlobName);
            var blobSas = blobRef.GetSharedAccessSignature(new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(12) // Allow time for blob copy and debugging, etc.
            });
            return blobRef.Uri + blobSas;
        }

        public void Dispose()
        {
            Azure.ResourceGroups.BeginDeleteByNameAsync(ResourceGroupName);
        }
    }
}
