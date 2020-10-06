using System;
using System.Linq;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils {
    public static class GitHubUtils {
        public static bool IsGithubTLD(string url) {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUri)) {
                return false;
            }

            return IsGithubTLD(parsedUri);
        }

        public static bool IsGithubTLD(Uri uri) {
            var mainDomain = HttpUtils.GetTLD(uri);

            return string.Equals(mainDomain, "github.com", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(mainDomain, "github.dev", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(mainDomain, "githubusercontent.com", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(mainDomain, "githubusercontent.localhost", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(mainDomain, "github.localhost", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
