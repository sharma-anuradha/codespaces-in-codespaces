using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public class RestrictIFrame : AddHeaderAttribute
    {
        public RestrictIFrame() : base("X-Frame-Options", "Deny")
        {
        }
    }
}
