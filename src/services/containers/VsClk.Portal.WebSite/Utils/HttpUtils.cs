using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public static class HttpUtils
    {
        public static string GetRequestOrigin(HttpRequest request)
        {
            request.Headers.TryGetValue("Origin", out var originValues);

            return originValues;
        }

        public static string FilterFirstHostSubdomain(string host)
        {
            return FilterFirstUrlSubdomain($"https://{host}");
        }

        public static string FilterFirstUrlSubdomain(string url)
        {
            var split = GetHost(url).Split(".");
            var parentDomainSplit = split.Skip(1);
            var parentDomain = String.Join(".", parentDomainSplit);

            return parentDomain;
        }

        public static string GetHost(string url)
        {
            var uri = new Uri(url);

            return uri.Host;
        }
    }
}
