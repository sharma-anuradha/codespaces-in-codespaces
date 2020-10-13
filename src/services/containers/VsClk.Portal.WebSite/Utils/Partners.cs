using System;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public class WorkbenchCspPartnerEndpoints
    {
        public string ProxyApiEndpoint { get; set; }
        public string FaviconsEndpoint { get; set; }
    }

    public static class Partners
    {
        public const string GitHub = "GitHub";
        public const string VSOnline = "VSOnline";
        public const string Salesforce = "Salesforce";

        public readonly static string GitHubHostTLD = "github.dev";
        public readonly static string GitHubOriginTLD = "github.com";
        public readonly static string GitHubReviewLabOriginTLD = "review-lab.github.com";
        public readonly static string GitHubLocalhostTLD = "github.localhost";

        public readonly static string ForceOriginTLD = "force.com";
        public readonly static string SalesforceOriginTLD = "salesforce.com";
        public readonly static string SalesforceHostTLD = "builder.code.com";

        public static bool isValidGithubAuthRequestOrigin(
            string origin,
            bool isProductionEnvironment,
            IDiagnosticsLogger logger = null
        )
        {
            var host = HttpUtils.GetHost(origin);
            var parentDomain = HttpUtils.FilterFirstUrlSubdomain(origin);

            if (parentDomain == GitHubReviewLabOriginTLD)
            {
                return true;
            }

            if (isProductionEnvironment)
            {
                var doesMatch = (host == GitHubOriginTLD);
                logger?
                    .FluentAddValue("Status", $"Production GitHub, matches github origin?: {doesMatch}")
                    .FluentAddValue("Result", doesMatch)
                    .LogInfo("platform_authentication_origin_validation_prod_github");

                return doesMatch;
            }

            var result = (
                host == GitHubOriginTLD ||
                host == GitHubLocalhostTLD
            );

            logger?
                .FluentAddValue("Status", $"Non-production GitHub, matches github origin?: {result}")
                .FluentAddValue("Result", result)
                .LogInfo("platform_authentication_origin_validation_nonprod_github");

            return result;
        }

        public static bool isValidGithubAuthRequestHost(
            string host,
            bool isProductionEnvironment,
            IDiagnosticsLogger logger = null
        )
        {
            var parentDomain = HttpUtils.FilterFirstHostSubdomain(host);
            if (isProductionEnvironment)
            {
                var doesMatch = (parentDomain == GitHubHostTLD);

                logger?
                    .FluentAddValue("Status", $"Production GitHub, matches github host?: {doesMatch}")
                    .FluentAddValue("Result", doesMatch)
                    .LogInfo("platform_authentication_host_validation_prod_github");

                return doesMatch;
            }

            var isValidHost = (
                $"ppe.{GitHubHostTLD}" == parentDomain ||
                $"dev.{GitHubHostTLD}" == parentDomain
            );

            logger?
                .FluentAddValue("Status", $"Non-production GitHub, matches github host?: {isValidHost}")
                .FluentAddValue("Result", isValidHost)
                .LogInfo("platform_authentication_host_validation_nonprod_github");

            return isValidHost;
        }

        public static bool isValidSalesforceAuthRequestOrigin(
            string origin,
            IDiagnosticsLogger logger = null
        )
        {
            var originHost = HttpUtils.GetHost(origin);
            var result = originHost.EndsWith(SalesforceOriginTLD) ||
                         originHost.EndsWith(ForceOriginTLD);

            logger?
                .FluentAddValue("Status", $"Salesforce, matches salesforce origin?: {result}")
                .FluentAddValue("Result", result)
                .LogInfo("platform_authentication_origin_validation_salesforce");

            return result;
        }

        public static bool IsGithubWorkbenchTLD(string host)
        {
            return host.EndsWith(GitHubHostTLD) ||
                    host.EndsWith(GitHubLocalhostTLD);
        }

        public static bool IsSalesforceWorkbenchTLD(string host, bool isProduction)
        {
            if (isProduction) {
                var parentDomain = HttpUtils.FilterFirstHostSubdomain(host);
                return (parentDomain == SalesforceHostTLD);
            }

            return host.EndsWith(SalesforceHostTLD);
        }

        public static bool IsValidAuthRequestOrigin(
            string origin,
            string host,
            bool isProduction,
            bool isLocal,
            IDiagnosticsLogger logger = null
        )
        {
            if (isLocal && isProduction)
            {
                throw new ArgumentException("isProduction and isLocal are not possible at the same time.");
            }

            logger?
                .FluentAddValue("Status", $"Partner auth request Origin: {origin}")
                .FluentAddValue("Origin", origin)
                .LogInfo("platform_authentication_partner_origin");

            logger?
                .FluentAddValue("Status", $"Partner auth request Host: {host}")
                .FluentAddValue("Host", host)
                .LogInfo("platform_authentication_partner_host");

            // cannot get request `Origin`, not valid to prevent
            // tricks with `null` origin in the browser
            if (
                string.IsNullOrWhiteSpace(origin) ||
                string.IsNullOrWhiteSpace(host)
            )
            {
                return false;
            }

            // allow any domain locally
            if (isLocal)
            {
                logger?
                    .FluentAddValue("Status", "Local portal, allowing any origin locally.")
                    .LogInfo("platform_authentication_local_portal");

                if (Partners.IsSalesforceWorkbenchTLD(host, false)) {
                    return isValidSalesforceAuthRequestOrigin(origin);
                }

                return true;
            }

            if (Partners.IsGithubWorkbenchTLD(host))
            {
                logger?
                    .FluentAddValue("Status", $"Checking GitHub origin and host.")
                    .LogInfo("platform_authentication_checking_github_request");

                return Partners.isValidGithubAuthRequestOrigin(origin, isProduction, logger) &&
                        Partners.isValidGithubAuthRequestHost(host, isProduction, logger);
            }

            if (Partners.IsSalesforceWorkbenchTLD(host, isProduction))
            {
                logger?
                    .FluentAddValue("Status", $"Checking Salesforce origin and host.")
                    .LogInfo("platform_authentication_checking_salesforce_request");

                return Partners.isValidSalesforceAuthRequestOrigin(origin, logger);
            }

            logger?
                .FluentAddValue("Status", $"Unknown partner.")
                .LogInfo("platform_authentication_invalid_request");

            return false;
        }
    }
}
