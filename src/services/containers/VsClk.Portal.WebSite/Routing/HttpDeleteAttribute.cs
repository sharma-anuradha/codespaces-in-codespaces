using System;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Routing
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class HttpDeleteAttribute : HttpMethodsAttribute
    {
        private static readonly string[] methods = new[] { "DELETE" };

        public HttpDeleteAttribute(string template, string host, params string[] additionalHosts)
            : base(methods, template, host, additionalHosts)
        {
        }
    }
}
