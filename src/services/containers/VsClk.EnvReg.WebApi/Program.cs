using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi
{
    public class Program
    {
        public static string ServiceName = "ENVREG";

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
                        .AddEnvironmentVariables(ServiceName + "_");    // ENVREG_AppSettings__*
                })
                .UseStartup<Startup>();
    }
}