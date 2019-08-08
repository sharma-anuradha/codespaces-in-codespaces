// <copyright file="AzureDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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

        public IAzure GetAzureClient(Guid subscriptionId)
        {
            var credentials = SdkContext.AzureCredentialsFactory
                .FromFile(authFile);

            return Azure.Management.Fluent.Azure.Authenticate(credentials).WithSubscription(subscriptionId.ToString());
        }

    }
}
