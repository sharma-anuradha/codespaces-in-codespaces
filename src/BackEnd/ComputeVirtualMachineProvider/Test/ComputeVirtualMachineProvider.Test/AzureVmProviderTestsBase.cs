// <copyright file="AzureVmProviderTestsBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Test
{
    public class AzureVmProviderTestsBase : IDisposable
    {
        public IConfigurationRoot Config { get; }
        public Guid SubscriptionId { get; }
        public string ResourceGroupName { get; }
        public AzureLocation Location { get; }
        public string AuthFilePath { get; }

        public AzureVmProviderTestsBase()
        {
            Config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
            SubscriptionId = Guid.Parse(Config["AZURE_SUBSCRIPTION"]);
            ResourceGroupName = "test-vm-rg-001";
            Location = AzureLocation.WestUs2;
            AuthFilePath = Config["AZURE_AUTH_LOCATION"];
        }

        public void Dispose()
        {
        }
    }
}
