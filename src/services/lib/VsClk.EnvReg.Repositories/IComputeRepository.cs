using System.Collections.Generic;
using System.Threading.Tasks;
using VsClk.EnvReg.Models.DataStore.Compute;

namespace VsClk.EnvReg.Repositories
{
    public interface IComputeRepository
    {
        Task<List<ComputeTargetResponse>> GetTargets();

        Task<ComputeResourceResponse> AddResource(string computeTargetId, ComputeServiceRequest computeServiceRequest);

        Task DeleteResource(string connectionComputeTargetId, string connectionComputeId);
    }
}
