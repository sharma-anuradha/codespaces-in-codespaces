using System.Threading.Tasks;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Repositories
{
    public interface IStorageManager 
    {
        Task<FileShare> CreateFileShareForEnvironmentAsync(FileShareEnvironmentInfo environmentInfo, IDiagnosticsLogger logger = null);
    }
}