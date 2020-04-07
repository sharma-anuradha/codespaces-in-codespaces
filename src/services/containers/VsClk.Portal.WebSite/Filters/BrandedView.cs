using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Filters
{
    public class BrandedView : Attribute, IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.Controller is Controller controller)
            {
                // Scheme doesn't matter for TLD checks
                var hostString = $"https://{context.HttpContext.Request.Host.ToString()}/";
                if (GitHubUtils.IsGithubTLD(hostString))
                {
                    controller.ViewData["RenderBranding"] = "GitHub";
                }
            }
        }
    }
}