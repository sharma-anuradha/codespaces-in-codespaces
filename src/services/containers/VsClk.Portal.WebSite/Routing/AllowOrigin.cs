using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Routing
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class AllowOrigin: Attribute, IActionConstraint
    {
        private IReadOnlyList<Uri> AllowedOrigins { get; }

        private bool AllowNoOrigin { get; set; } = false;

        public AllowOrigin(string origin, params string[] additionalOrigins)
        {
            string[] origins;
            if (additionalOrigins != null && additionalOrigins.Length != 0)
            {
                origins = new string[additionalOrigins.Length + 1];
                origins[0] = origin;
                additionalOrigins.CopyTo(origins, 1);
            }
            else
            {
                origins = new[] { origin };
            }

            AllowedOrigins = origins.Select(o => new Uri(o, UriKind.Absolute)).ToArray();
        }
        
        public int Order { get; set; }

        public bool Accept(ActionConstraintContext context)
        {
            if (!context.RouteContext.HttpContext.Request.Headers.TryGetValue(HeaderNames.Origin, out var headerValues))
            {
                return AllowNoOrigin;
            }

            var origin = headerValues.SingleOrDefault();
            if (origin == default || !Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
            {
                return false;
            }

            return AllowedOrigins.Any(allowedOrigin => allowedOrigin == originUri);
        }
    }
}
