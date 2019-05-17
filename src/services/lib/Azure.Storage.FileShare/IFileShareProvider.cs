using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Azure.Storage.FileShare
{
    public interface IFileShareProvider
    {
        Task<bool> TryCreateFileShareWithNameAsync(string name, IDiagnosticsLogger logger = null);
        Task DeleteFileShareWithNameAsync(string name, IDiagnosticsLogger logger = null);
    }
}