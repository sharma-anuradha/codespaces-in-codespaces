using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    public class Program
    {
        public static string ServiceName = "PORTAL";

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Setup AppSettings configuration
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                        .AddJsonFile($"appsettings.secrets.json", optional: true)
                        .AddEnvironmentVariables(ServiceName + "_");    // PORTAL_AppSettings__*
                })
                .UseStartup<Startup>();
    }
}
