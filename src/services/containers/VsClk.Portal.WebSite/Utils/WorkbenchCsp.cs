using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using SmartFormat;

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
            prefetch-src 'self';
            script-src
                'self'
                'unsafe-eval'
                'nonce-{InlineJavaScriptNonce}'
            ;
            style-src
                'self'
                'unsafe-inline'
            ;
            img-src
                'self'
                data:
                https://*.gallerycdn.vsassets.io
                {PartnerFaviconsEndpoint}
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
            ;
            font-src 'self';
            frame-src https://*.vscode-webview-test.com;
        ";

        public static Tuple<string, WorkbenchCspDynamicAttributes> GetCsp(
            AppSettings appSettings,
            IHostEnvironment hostEnvironment,
            string requestUrl
            )
        {
            // interpolate the CSP string placeholders
            var relayEndpoints = GetLiveShareRelayEndpoints(hostEnvironment);
            var inlineJavaScriptNonce = RuntimeUtils.GetNonceBase64();

            var attributes = new WorkbenchCspDynamicAttributes()
                {
                    RelayEndpoints = relayEndpoints,
                    InlineJavaScriptNonce = inlineJavaScriptNonce,
                    LiveShareEndpoint = appSettings.LiveShareEndpoint,
                    WildcardApiEndpoint = HttpUtils.ReplaceWithWildcardSubdomain(appSettings.Domain),
                    PartnerProxyApiEndpoint = GetPartnerApiProxyEndpoint(requestUrl),
                    PartnerFaviconsEndpoint = GetPartnerFaviconsEndpoint(requestUrl),
                    PartnerPortForwardingEndpoint = GetPartnerPortForwardingManagementEndpoint(requestUrl, appSettings),
                };

            var csp = Smart.Format(GetCspString(), attributes);
            return Tuple.Create(
                csp,
                attributes
            );
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
