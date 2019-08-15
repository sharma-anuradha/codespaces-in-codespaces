using System;
using System.Threading.Tasks;
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
        public AzureLocation Location { get; }
        public string AuthFilePath { get; }

        private AzureDeploymentHelper azureDeploymentHelper;

        public AzureVmProviderTestsBase()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("test-settings.json")
                .Build();
            SubscriptionId = Guid.Parse(config["AZURE_SUBSCRIPTION"]);
            ResourceGroupName = $"test-vm-{Guid.NewGuid()}";
            Location = AzureLocation.WestUs2;
            AuthFilePath = config["AZURE_AUTH_LOCATION"];
            azureDeploymentHelper = new AzureDeploymentHelper(new AzureClientFactoryMock(AuthFilePath));
            azureDeploymentHelper.CreateResourceGroupAsync(SubscriptionId, ResourceGroupName, AzureLocation.EastUs).Wait();
        }

        public void Dispose()
        {
            (azureDeploymentHelper.DeleteResourceGroupAsync(SubscriptionId, ResourceGroupName)).Wait();
        }
    }
}
