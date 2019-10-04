using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public class WorkSpaceInfo
    {
        private static HttpClient client = new HttpClient();

        public static async Task<string> GetWorkSpaceOwner(string token, string sessionId, string liveShareEndpoint)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync(liveShareEndpoint + Constants.LiveShareWorkspaceRoute + sessionId);

            if (!response.IsSuccessStatusCode) 
            { 
                return null; 
            }

            HttpContent content = response.Content;
            var data = await content.ReadAsAsync<JObject>();

            return data.Value<string>("ownerId");
        }
    }
}
