using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public interface IWorkspaceInfo
    {
        Task<string> GetWorkSpaceOwner(string token, string sessionId, string liveShareEndpoint);
    }
}