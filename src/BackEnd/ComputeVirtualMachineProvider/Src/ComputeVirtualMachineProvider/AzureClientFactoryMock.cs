// <copyright file="AzureClientFactoryMock.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public class AzureClientFactoryMock : IAzureClientFactory
    {
        private readonly string authFile;

        public AzureClientFactoryMock(string authFile)
        {
            this.authFile = authFile;
        }

        public async Task<IAzure> GetAzureClientAsync(Guid subscriptionId)
        {
            var credentials = SdkContext.AzureCredentialsFactory
                .FromFile(authFile);

            return await Task.FromResult(Azure.Management.Fluent.Azure.Authenticate(credentials).WithSubscription(subscriptionId.ToString()));
        }

    }
}
