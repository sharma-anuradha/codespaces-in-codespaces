using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using SmartFormat;
using UAParser;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public static class WorkbenchCSP
    {
        // see `CSP.md` for more info
        public static readonly string WorkbenchPageCSP = @"
            report-uri /csp-report;
            upgrade-insecure-requests;
            block-all-mixed-content;
            default-src 'none';
            base-uri 'self';
            manifest-src 'self';
            script-src
                'self'
                'unsafe-eval'
                'nonce-{InlineJavaScriptNonce}'
                https://vscodeweb.azureedge.net/insider/
                https://vscodeweb.azureedge.net/stable/
            ;
            style-src
                'self'
                'unsafe-inline'
                https://vscodeweb.azureedge.net/insider/
                https://vscodeweb.azureedge.net/stable/
            ;
            img-src
                'self'
                data:
                https://*.gallerycdn.vsassets.io
                {PartnerFaviconsEndpoint}
                https://vscodeweb.azureedge.net/insider/
                https://vscodeweb.azureedge.net/stable/
            ;
            connect-src
                'self'
                {PartnerPortForwardingEndpoint}
                {PartnerProxyApiEndpoint}
                {WildcardApiEndpoint}
                {LiveShareEndpoint}
                {RelayEndpoints}
                https://*.gallerycdn.vsassets.io
                https://vortex.data.microsoft.com/collect/v1
                https://vscode-sync.trafficmanager.net/
                https://vscode-sync-insiders.trafficmanager.net/
                https://marketplace.visualstudio.com/_apis/public/gallery/
                https://az764295.vo.msecnd.net/experiments/vscode-experiments.json
                https://vscodeexperiments.azureedge.net/experiments/vscode-experiments.json
                https://vscodeweb.azureedge.net/insider/
                https://vscodeweb.azureedge.net/stable/
                https://default.exp-tas.com/vscode/ab
            ;
            font-src
                'self'
                https://vscodeweb.azureedge.net/insider/
                https://vscodeweb.azureedge.net/stable/
            ;
            frame-src
                https://*.vscode-webview-test.com
                https://vscodeweb.azureedge.net/insider/
                https://vscodeweb.azureedge.net/stable/
            ;
            {WorkerSrcOrClildSrc}
                {ServiceWorkerEndpoint}
                blob:
            ;
        ";

        public static Tuple<string, WorkbenchCspDynamicAttributes> GetCsp(
            AppSettings appSettings,
            IHostEnvironment hostEnvironment,
            HttpRequest request
            )
        {
            // interpolate the CSP string placeholders
            var relayEndpoints = GetLiveShareRelayEndpoints(hostEnvironment);
            var inlineJavaScriptNonce = RuntimeUtils.GetNonceBase64();
            var requestUrl = HttpUtils.GetAbsoluteUri(request);

            var attributes = new WorkbenchCspDynamicAttributes()
                {
                    RelayEndpoints = relayEndpoints,
                    InlineJavaScriptNonce = inlineJavaScriptNonce,
                    LiveShareEndpoint = appSettings.LiveShareEndpoint,
                    WildcardApiEndpoint = HttpUtils.ReplaceWithWildcardSubdomain(appSettings.Domain),
                    PartnerProxyApiEndpoint = GetPartnerApiProxyEndpoint(requestUrl),
                    PartnerFaviconsEndpoint = GetPartnerFaviconsEndpoint(requestUrl),
                    ServiceWorkerEndpoint = GetServiceWorkerEndpoint(requestUrl),
                    PartnerPortForwardingEndpoint = GetPartnerPortForwardingManagementEndpoint(requestUrl, appSettings),
                    WorkerSrcOrClildSrc = GetWorkerSrcOrChildSrc(request),
                };

            var csp = Smart.Format(GetCspString(), attributes);
            return Tuple.Create(
                csp,
                attributes
            );
        }

        private static string GetWorkerSrcOrChildSrc(HttpRequest request)
        {
            if (request.Headers.ContainsKey(HeaderNames.UserAgent))
            {
                try
                {
                    var userAgent = request.Headers[HeaderNames.UserAgent];
                    var ua = Parser.GetDefault().Parse(Convert.ToString(userAgent[0]));
                    if (ua.UA.Family.ToLower().Contains("safari"))
                    {
                        // Safari does not support worker-src
                        return "child-src";
                    }
                }
                catch
                {
                    // ignore any exceptions and return default
                }
            }

            return "worker-src";
        }

        private static string GetServiceWorkerEndpoint(string requestUrl)
        {
            if (GitHubUtils.IsGithubTLD(requestUrl))
            {
                if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out var parsedUri)) {
                    return "";
                }
                return $"https://{parsedUri.Host}/service-worker.js";
            }

            return "";
        }


        private static string GetPartnerApiProxyEndpoint(string requestUrl)
        {
            if (GitHubUtils.IsGithubTLD(requestUrl))
            {
                return WorkbenchCspHelpers.GitHubCspEndpoints.ProxyApiEndpoint;
            }

            return "";
        }

        private static string GetPartnerFaviconsEndpoint(string requestUrl)
        {
            if (GitHubUtils.IsGithubTLD(requestUrl))
            {
                return WorkbenchCspHelpers.GitHubCspEndpoints.FaviconsEndpoint;
            }

            return "";
        }

        private static string GetPartnerPortForwardingManagementEndpoint
        (
            string requestUrl,
            AppSettings appSettings
        )
        {
            if (GitHubUtils.IsGithubTLD(requestUrl))
            {
                return appSettings.GitHubPortForwardingManagementEndpoint;
            }

            return appSettings.PortForwardingManagementEndpoint;
        }

        private static string GetCspString()
        {
            var csp = WorkbenchPageCSP;

            // remove new lines since the control characters are not permitted in headers
            csp = csp.Replace(System.Environment.NewLine, " ");
            // remove multiple spaces present since we use the multi-line string above
            csp = Regex.Replace(csp, @"\s+", " ", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            return csp;
        }

        private static string GetLiveShareRelayEndpoints(
            IHostEnvironment env
        )
        {
            var endpoints = (env.IsProduction())
                ? WorkbenchCspHelpers.LiveShareProdRelayEndpoints
                : WorkbenchCspHelpers.LiveShareDevRelayEndpoints;

            var fullRelayEndpoints = endpoints.Select(item => $"wss://vsls-{item}.servicebus.windows.net");

            return String.Join(" ", fullRelayEndpoints);
        }
    }
}
