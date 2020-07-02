using System;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Routing
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class HttpPatchAttribute : HttpMethodsAttribute
    {
        private static readonly string[] methods = new[] { "PATCH" };

        public HttpPatchAttribute(string template, string host, params string[] additionalHosts)
            : base(methods, template, host, additionalHosts)
        {
        }
    }
}
