using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils {
    public static class HttpUtils {
        public static string GetTLD(Uri uri)
        {
            var hostString = uri.Host.ToString();
            var locationSplit = hostString.Split(".");
            var mainDomain = string.Join(".", locationSplit.TakeLast(2));

            return mainDomain;
        }

        public static string GetTLD(string currentUrl)
        {
            var uri = new UriBuilder(currentUrl);
            return HttpUtils.GetTLD(uri.Uri);
        }

        public static string ReplaceWithWildcardSubdomain(string currentUrl)
        {
            var uri = new UriBuilder(currentUrl);

            var hostSplitCharacter = '.';
            var hostSplit = uri.Host.Split(hostSplitCharacter);
            hostSplit[0] = "*";

            uri.Host = String.Join(hostSplitCharacter, hostSplit);

            return uri.ToString();
        }

        public static string GetAbsoluteUri(HttpRequest request)
        {
            var uri = new UriBuilder();
            uri.Scheme = request.Scheme;
            uri.Path = request.Path;
            uri.Host = request.Host.Value;
            uri.Query = request.QueryString.Value;

            if (request.Host.Port.HasValue) {
                uri.Port = request.Host.Port.Value;
            }

            return uri.ToString();
        }

        public static async Task<string> ReadFileContentsAsync(Stream fileStream)
        {
            using (StreamReader streamReader = new StreamReader(fileStream))
            {
                return await streamReader.ReadToEndAsync();
            }
        }

        public static async Task<string> ReadFileContentsAsync(string filePath)
        {
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                return await streamReader.ReadToEndAsync();
            }
        }

        public static string GetRequestHeader(HttpRequest request, string headerName)
        {
            request.Headers.TryGetValue(headerName, out var originValues);

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
