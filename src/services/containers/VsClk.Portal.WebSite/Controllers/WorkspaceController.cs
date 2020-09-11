using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.Extensions.Hosting;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class WorkspaceController : Controller
    {
        private static AppSettings AppSettings { get; set; }
        private IHostEnvironment HostEnvironment { get; }

        public WorkspaceController(AppSettings appSettings, IHostEnvironment hostEnvironment)
        {
            AppSettings = appSettings;
            HostEnvironment = hostEnvironment;
        }

        [HttpGet("~/codespace")]
        [HttpGet("~/workspace/{id}")]
        [Routing.HttpGet(
            "~/",
            "*.github.dev",
            "*.github.localhost",
            "*.codespaces.visualstudio.com",
            "*.dev.builder.code.com",
            "*.ppe.builder.code.com",
            "*.builder.code.com",
            "*.local.builder.code.com"
        )]
        public Task<ActionResult> Index() => FetchStaticAsset("workbench.html", "text/html");

        private async Task<ActionResult> FetchStaticAsset(string path, string mediaType)
        {
            // Locally we don't produce the physical file, so we grab it from the portal itself.
            // The portal runs on https://localhost:3030 only right now, because of authentication.
            if (AppSettings.IsLocal)
            {
                var client = new HttpClient();
                var stream = await client.GetStreamAsync($"http://localhost:3030/{path}");

                return File(stream, mediaType);
            }

            // The static files in test are limited to the ones that don't need to be built.
            if (AppSettings.IsTest)
            {
                var assetPhysicalPath = Path.Combine(HostEnvironment.ContentRootPath,
                    "ClientApp", "public", path);

                return PhysicalFile(assetPhysicalPath, mediaType);
            }

            var asset = Path.Combine(Directory.GetCurrentDirectory(),
                "ClientApp", "build", path);

            return PhysicalFile(asset, mediaType);
        }


        [HttpGet("~/settings-sync")]
        public async Task<IActionResult> Authorize(
            [FromQuery(Name = "resourceId")] string resourceId
        )
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                return BadRequest("No 'resourceId' query param set.");
            }

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://vscode-sync.trafficmanager.net/v1/resource/{resourceId}/latest");
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


            HttpClient client = new HttpClient();

            Request.Headers.TryGetValue("authorization", out var authHeader);
            if (string.IsNullOrWhiteSpace(authHeader))
            {
                return BadRequest("No 'authorization' header set.");
            }

            client.DefaultRequestHeaders.Add("authorization", authHeader.ToString());

            Request.Headers.TryGetValue("x-account-type", out var accountType);
            if (string.IsNullOrWhiteSpace(accountType))
            {
                return BadRequest("No 'x-account-type' header set.");
            }

            client.DefaultRequestHeaders.Add("x-account-type", accountType.ToString());

            HttpResponseMessage response = await client.SendAsync(requestMessage);

            var content = await response.Content.ReadAsStringAsync();

            Response.StatusCode = (int)response.StatusCode;

            return Content(content, "application/json");
        }
    }
}