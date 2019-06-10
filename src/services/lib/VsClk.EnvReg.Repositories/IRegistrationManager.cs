using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsSaaS.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VsClk.EnvReg.Repositories
{
    public interface IRegistrationManager
    {
        Task<EnvironmentRegistration> GetAsync(
            string id,
            string ownerId,
            IDiagnosticsLogger logger);

        Task<IEnumerable<EnvironmentRegistration>> GetListByOwnerAsync(
            string ownerId,
            IDiagnosticsLogger logger);

        Task<EnvironmentRegistration> RegisterAsync(
            EnvironmentRegistration model,
            EnvironmentRegistrationOptions options,
            string ownerId,
            string accessToken,
            IDiagnosticsLogger logger);

        Task<bool> DeleteAsync(
            string id,
            string ownerId,
            IDiagnosticsLogger logger);

        Task<EnvironmentRegistration> CallbackUpdateAsync(
            string id,
            EnvironmentRegistrationCallbackOptions options,
            string ownerId,
            IDiagnosticsLogger logger);

    }
}
