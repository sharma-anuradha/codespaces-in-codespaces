using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SmartFormat;
using SmartFormat.Core.Settings;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Controllers
{
    public class WorkbenchController : Controller
    {
        private AppSettings AppSettings { get; }
        private IHostEnvironment HostEnvironment { get; }
        private IMemoryCache Cache { get; }

        public WorkbenchController(
            AppSettings appSettings,
            IHostEnvironment hostEnvironment,
            IMemoryCache memoryCache
            )
        {
            AppSettings = appSettings;
            HostEnvironment = hostEnvironment;
            Cache = memoryCache;
        }

        [RestrictIFrame]
        [HttpGet("~/codespace")]
        [HttpGet("~/workspace/{id}")]
        [Routing.HttpGet(
            "~/",
            "*.github.dev",
            "*.github.localhost",
            "*.codespaces.visualstudio.com",
            "*.local.builder.code.com",
            "*.dev.builder.code.com",
            "*.ppe.builder.code.com",
            "*.builder.code.com"
        )]
        public Task<ActionResult> Index()
        {
            var csp = WorkbenchCSP.GetCsp(
                AppSettings,
                HostEnvironment,
                HttpContext.Request
            );

            HttpContext.Response.Headers.Add("Content-Security-Policy", csp.Item1);

            return FetchWorkbenchPage("workbench.html", "text/html", csp.Item2);
        }

        private async Task<string> GetWorkbenchFileContents(string path)
        {
            var key = $"workbench-page-html-cache:{path}";
            Cache.TryGetValue<string>(key, out string fileContents);

            if (!string.IsNullOrWhiteSpace(fileContents))
            {
                return fileContents;
            }

            // Locally we don't produce the physical file, so we grab it from the portal itself.
            // The portal runs on https://localhost:3030 only right now, because of authentication.
            if (AppSettings.IsLocal)
            {
                using (var client = new HttpClient()) {
                    var stream = await client.GetStreamAsync($"http://localhost:3030/{path}");

                    var data = await HttpUtils.ReadFileContentsAsync(stream);
                    Cache.Set(key, data);

                    return data;
                }
            }

            var workbenchFileFolderPath = (AppSettings.IsTest)
                ? "public"
                : "build";

            var workbenchFilePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "ClientApp",
                workbenchFileFolderPath,
                path
            );

            var contents = await HttpUtils.ReadFileContentsAsync(workbenchFilePath);
            Cache.Set(key, contents);

            return contents;
        }

        private async Task<ActionResult> FetchWorkbenchPage(
            string path,
            string mediaType,
            WorkbenchCspDynamicAttributes attrs
        )
        {
            var workbenchFileContents = await GetWorkbenchFileContents(path);
            // since the html file might contain CSS style blocks `{}`
            // we need to ignore the errors for attemting to parse/evaluate such blocks
            Smart.Default.Settings.ParseErrorAction = ErrorAction.MaintainTokens;
            Smart.Default.Settings.FormatErrorAction = ErrorAction.MaintainTokens;

            return new ContentResult
            {
                ContentType = mediaType,
                Content = Smart.Format(workbenchFileContents, attrs),
            };
        }

        [HttpPost("~/csp-report")]
        public IActionResult CspReport(
            [FromBody] CspReportRequest request,
            [FromServices] IDiagnosticsLogger logger
        )
        {
            var cspReport = request.CspReport;
            var logString = $"CSP Violation: {HttpUtils.GetTLD(cspReport.DocumentUri)}, {cspReport.BlockedUri}, {cspReport.ViolatedDirective}, {cspReport.OriginalPolicy}";

            if (!string.IsNullOrWhiteSpace(cspReport.SourceFile))
            {
                var sourceFile = new UriBuilder(cspReport.SourceFile);
                logString += $", {sourceFile.Path}:{cspReport.LineNumber}:{cspReport.ColumnNumber}";
            }

            logger.LogWarning(logString);

            return Ok();
        }
    }
}
