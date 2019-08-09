// <copyright file="IAzureClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public interface IAzureClientFactory
    {
        Task<IAzure> GetAzureClientAsync(Guid subscriptionId);
    }
}
