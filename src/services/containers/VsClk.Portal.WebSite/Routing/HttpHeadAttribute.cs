using System;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Routing
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class HttpHeadAttribute : HttpMethodsAttribute
    {
        private static readonly string[] methods = new[] { "HEAD" };

        public HttpHeadAttribute(string template, string host, params string[] additionalHosts)
            : base(methods, template, host, additionalHosts)
        {
        }
    }
}
