using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Routing
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class HttpMethodsAttribute : Attribute, IActionHttpMethodProvider, IRouteTemplateProvider, IHostMetadata
    {
        public HttpMethodsAttribute(string[] methods, string template, string host, params string[] additionalHosts)
        {
            HttpMethods = methods;
            Template = template;
            if (additionalHosts != null && additionalHosts.Length != 0)
            {
                var hosts = new string[additionalHosts.Length + 1];
                hosts[0] = host;
                additionalHosts.CopyTo(hosts, 1);

                Hosts = hosts;
            }
            else
            {
                Hosts = new[] { host };
            }
        }

        public string Name { get; set; }

        public int? Order { get; set; }

        public string Template { get; }

        public IEnumerable<string> HttpMethods { get; }

        public IReadOnlyList<string> Hosts { get; }
    }
}
