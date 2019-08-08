using System;
using Microsoft.Extensions.Configuration;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Test
{
    public class AzureVmProviderTestsBase : IDisposable
    {
        public Guid SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public string AuthFilePath { get; }

        public AzureVmProviderTestsBase()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("test-settings.json")
                .Build();
            SubscriptionId = Guid.Parse(config["AZURE_SUBSCRIPTION"]);
            ResourceGroupName = $"test-vm-{Guid.NewGuid()}";
            AuthFilePath = config["AZURE_AUTH_LOCATION"];
        }

        public void Dispose()
        {
            var azureDeploymentManager = new AzureDeploymentManager(new AzureClientFactoryMock(AuthFilePath));
            _ = azureDeploymentManager.DeleteVMAsync(new ResourceId(ResourceType.ComputeVM, Guid.NewGuid(), SubscriptionId, ResourceGroupName, AzureLocation.EastUs)).Result;
        }
    }
}
