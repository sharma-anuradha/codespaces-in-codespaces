using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public class WorkSpaceInfo
    {
        private static HttpClient client = new HttpClient();
        public static AppSettings AppSettings { get; set; }

        public static async Task<string> GetWorkSpaceOwner(string token, string sessionId)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync(Constants.LiveShareEndPoint + sessionId);
            HttpContent content = response.Content;
            var data = await content.ReadAsAsync<JObject>();

            return data.Value<string>("ownerId");
        }
    }
}
