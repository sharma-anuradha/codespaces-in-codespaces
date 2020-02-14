using Microsoft.AspNetCore.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Test
{
    public static class MockHttpContext
    {
        public static HttpContext Create()
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("testhost");
            context.Request.PathBase = new PathString(string.Empty);
            context.Request.Path = new PathString("/test/path");
            context.Request.QueryString = new QueryString(string.Empty);

            return context;
        }
    }
}
