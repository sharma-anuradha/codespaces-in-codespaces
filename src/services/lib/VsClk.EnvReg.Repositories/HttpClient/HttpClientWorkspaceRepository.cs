using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using VsClk.EnvReg.Models.DataStore.Workspace;
using VsClk.EnvReg.Repositories.Support.HttpClient;

namespace VsClk.EnvReg.Repositories.HttpClient
{
    public class HttpClientWorkspaceRepository : IWorkspaceRepository
    {
        const string Path = "workspace";

        public HttpClientWorkspaceRepository(IHttpClientProvider httpClientProvider)
        {
            HttpClientProvider = httpClientProvider;
        }

        private IHttpClientProvider HttpClientProvider { get; }

        public async Task<WorkspaceResponse> CreateAsync(WorkspaceRequest workspace)
        {
            var response = await HttpClientProvider.WorkspaceServiceClient.PostAsync(Path, workspace, new JsonMediaTypeFormatter());
            await response.ThrowIfFailedAsync();
            var workspaceResponse = await response.Content.ReadAsAsync<WorkspaceResponse>();
            return workspaceResponse;
        }

        public async Task DeleteAsync(string workspaceId)
        {
            var response = await HttpClientProvider.WorkspaceServiceClient.DeleteAsync($"{Path}/{workspaceId}");
            if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                // The call may fail, as the workspace may already been cleaned up. Ignore failure response.
                await response.ThrowIfFailedAsync();
            }
        }

        public async Task<WorkspaceResponse> GetStatusAsync(string workspaceId)
        {
            var response = await HttpClientProvider.WorkspaceServiceClient.GetAsync($"{Path}/{workspaceId}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // If deleted or missing, handle it.
                return null;
            }

            await response.ThrowIfFailedAsync();
            var workspaceResponse = await response.Content.ReadAsAsync<WorkspaceResponse>();
            return workspaceResponse;
        }
    }
}