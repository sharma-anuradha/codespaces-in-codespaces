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
            ResourceGroupName = "test-vm-80baa231-d7e7-44c1-9ec5-3c274c322576";
            Location = AzureLocation.WestUs2;
            AuthFilePath = config["AZURE_AUTH_LOCATION"];
            azureDeploymentHelper = new AzureDeploymentHelper(new AzureClientFactoryMock(AuthFilePath));
        }

        public void Dispose()
        {
        }
    }
}
