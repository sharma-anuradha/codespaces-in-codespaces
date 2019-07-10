using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VsClk.EnvReg.Models.DataStore.Compute;
using VsClk.EnvReg.Repositories;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Repositories
{
#if DEBUG

    public class MockComputeRepository : IComputeRepository
    {
        // Map of computeId to request and response.
        private readonly Dictionary<string, Tuple<ComputeServiceRequest, ComputeResourceResponse>> store = new Dictionary<string, Tuple<ComputeServiceRequest, ComputeResourceResponse>>();

        public Task<ComputeResourceResponse> AddResourceAsync(string computeTargetId, ComputeServiceRequest computeServiceRequest)
        {
            var computeResource = new ComputeResourceResponse()
            {
                Created = DateTime.Now.ToString(),
                Id = Guid.NewGuid().ToString(),
                State = StateInfo.Provisioning.ToString()
            };

            this.store[computeResource.Id] = Tuple.Create(computeServiceRequest, computeResource);

            return Task.FromResult(computeResource);
        }

        public Task DeleteResourceAsync(string connectionComputeTargetId, string connectionComputeId)
        {
            this.store.Remove(connectionComputeId);
            return Task.CompletedTask;
        }

        public Task<List<ComputeTargetResponse>> GetTargetsAsync()
        {
            var result = new List<ComputeTargetResponse>();

            result.Add(new ComputeTargetResponse()
            {
                Created = DateTime.Now.ToString(),
                Id = Guid.NewGuid().ToString(),
                Name = "Mock",
                Properties = new Dictionary<string, dynamic>() { { "region", null } },
                State = "Available",
            });

            return Task.FromResult(result);
        }
    }

#endif // DEBUG
}
