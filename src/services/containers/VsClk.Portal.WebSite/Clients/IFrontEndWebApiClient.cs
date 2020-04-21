using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;


namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Clients
{
    public interface IFrontEndWebApiClient
    {
        Task<CloudEnvironmentResult> GetEnvironmentAsync(string environmentId, string token, IDiagnosticsLogger logger);
    }
}