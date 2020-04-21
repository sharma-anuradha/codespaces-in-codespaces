using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public static class PartnerExtensions
    {
        public static string GetPartner(this HttpContext context)
        {
            // Scheme doesn't matter for TLD checks
            if (GitHubUtils.IsGithubTLD($"https://{context.Request.Host.ToString()}"))
            {
                return Partners.GitHub;
            }
            // TODO: Add support for more partners.
            else
            {
                return Partners.VSOnline;
            }
        }
    }
}