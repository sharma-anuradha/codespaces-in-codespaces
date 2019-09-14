// <copyright file="MockTokenProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using System;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Test
{
    public class MockTokenProvider : IVirtualMachineTokenProvider
    {
        public Task<string> GenerateAsync(string identifier, IDiagnosticsLogger logger)
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }
    }
}
