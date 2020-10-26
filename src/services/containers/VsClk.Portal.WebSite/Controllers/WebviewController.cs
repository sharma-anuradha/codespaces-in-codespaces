using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.IO;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using System.Net.Http;
using System.Linq;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class WebviewController: Controller
    {
        private const string LoggingBaseName = "webview_controller";
        private readonly string CdnBaseUrl = "https://vscodeweb.azureedge.net/{0}/{1}/out/vs/workbench/contrib/webview/browser/pre/{2}";

        [HttpGet]
        [Route("/webview/{commitId}/{fileName}")]
        public async Task<IActionResult> Get(string commitId, string filename, [FromServices] IDiagnosticsLogger logger)
        {
            var mediaType = filename.EndsWith("html", StringComparison.OrdinalIgnoreCase) ?
                               "text/html" : "application/javascript";

            var result = await Task.WhenAll(GetFileStreamAsync("insider", commitId, filename), GetFileStreamAsync("stable", commitId, filename));

            if (result == null || !result.Any())
            {
                logger?
                .FluentAddValue("Result", $"Error while fetching files.")
                .LogError($"{LoggingBaseName}_get_failed");

                return NotFound($"Error while fetching files.");
            }

            var insiderResult = result.First();
            var stableResult = result.Last();

            if (insiderResult != null)
            {
                logger?
                .FluentAddValue("Result", $"{filename} - fetched.")
                .LogInfo($"{LoggingBaseName}_get_succeeded");

                return File(insiderResult, mediaType);
            }

            if (stableResult != null)
            {
                logger?
                .FluentAddValue("Result", $"{filename} - fetched.")
                .LogInfo($"{LoggingBaseName}_get_succeeded");

                return File(stableResult, mediaType);
            }

            logger?
                .FluentAddValue("Result", $"{filename} file not found.")
                .LogError($"{LoggingBaseName}_get_failed");
            return NotFound($"{filename} in commit: {commitId} not found.");
        }

        public async Task<Stream> GetFileStreamAsync(string quality, string commitId, string fileName)
        {
            var fileUrl = string.Format(CdnBaseUrl, quality, commitId, fileName);
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    return stream;
                }

                return null;
            }
        }
    }
}
