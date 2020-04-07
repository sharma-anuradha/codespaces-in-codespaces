
// export const isGithubTLD = (urlString: string): boolean => {
//     const url = new URL(urlString);
//     const locationSplit = url.hostname.split('.');
//     const mainDomain = locationSplit.slice(locationSplit.length - 2).join('.');
//     return mainDomain === 'github.com' || mainDomain === 'github.localhost';
// };

using System;
using System.Linq;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public static class GitHubUtils
    {
        public static bool IsGithubTLD(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUri))
            {
                return false;
            }

            var hostString = parsedUri.Host.ToString();
            var locationSplit = hostString.Split(".");
            var mainDomain = string.Join(".", locationSplit.TakeLast(2));
            return string.Equals(mainDomain, "github.com", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(mainDomain, "githubusercontent.com", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(mainDomain, "githubusercontent.localhost", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(mainDomain, "github.localhost", StringComparison.InvariantCultureIgnoreCase);

        }
    }
}