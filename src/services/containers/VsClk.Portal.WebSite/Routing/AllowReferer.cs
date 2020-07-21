using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Routing
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class AllowReferer: Attribute, IActionConstraint
    {
        private IReadOnlyList<Uri> AllowedReferers { get; }

        private bool AllowNoReferer { get; set; } = false;

        public AllowReferer(string referer, params string[] additionalReferers)
        {
            string[] referers;
            if (additionalReferers != null && additionalReferers.Length != 0)
            {
                referers = new string[additionalReferers.Length + 1];
                referers[0] = referer;
                additionalReferers.CopyTo(referers, 1);
            }
            else
            {
                referers = new[] { referer };
            }

            AllowedReferers = referers.Select(o => new Uri(o, UriKind.Absolute)).ToArray();
        }
        
        public int Order { get; set; }

        public bool Accept(ActionConstraintContext context)
        {
            // Trust me, you want this variable when you're debugging.
            var headers = context.RouteContext.HttpContext.Request.Headers;
            if (!headers.TryGetValue(HeaderNames.Referer, out var headerValues))
            {
                return AllowNoReferer;
            }

            var referer = headerValues.SingleOrDefault();
            if (referer == default || !Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            {
                return false;
            }

            return AllowedReferers.Any(allowedReferer => allowedReferer.Authority == refererUri.Authority);
        }
    }
}
