using Microsoft.AspNetCore.Hosting;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi
{
    public class Startup
    {
        private static readonly string ServiceNameValue = typeof(Startup).Namespace.ToUpperInvariant();

        public Startup(IHostingEnvironment hostingEnvironment)
        {
        }

        public string ServiceName => ServiceNameValue;
    }
}
