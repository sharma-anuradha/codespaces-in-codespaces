using System.Collections.Generic;
using System.Threading.Tasks;
using VsClk.EnvReg.Models.DataStore.Workspace;

namespace VsClk.EnvReg.Repositories
{
    public interface IWorkspaceRepository
    {
        Task<WorkspaceResponse> CreateAsync(WorkspaceRequest workspace);

        Task DeleteAsync(string workspaceId);
    }
}
