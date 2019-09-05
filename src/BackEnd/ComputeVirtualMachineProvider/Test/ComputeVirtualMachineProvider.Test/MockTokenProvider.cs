using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Test
{
    public class MockTokenProvider : IVSSaaSTokenProvider
    {
        public string Generate(string identifier)
        {
            return Guid.NewGuid().ToString();
        }
    }
}
