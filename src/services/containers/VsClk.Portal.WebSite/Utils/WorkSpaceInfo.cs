using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public class WorkspaceInfo : IWorkspaceInfo
    {
        private readonly HttpClient client = new HttpClient();

        public async Task<string> GetWorkSpaceOwner(string token, string sessionId, string liveShareEndpoint)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync(liveShareEndpoint + Constants.LiveShareWorkspaceRoute + sessionId);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            var owner = json.Value<string>("ownerId");
            return owner;
        }
    }
}
