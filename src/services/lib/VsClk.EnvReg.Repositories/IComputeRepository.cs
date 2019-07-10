using System.Collections.Generic;
using System.Threading.Tasks;
using VsClk.EnvReg.Models.DataStore.Compute;

namespace VsClk.EnvReg.Repositories
{
    public interface IComputeRepository
    {
        Task<List<ComputeTargetResponse>> GetTargetsAsync();

        Task<ComputeResourceResponse> AddResourceAsync(string computeTargetId, ComputeServiceRequest computeServiceRequest);

        Task DeleteResourceAsync(string connectionComputeTargetId, string connectionComputeId);
    }
}
