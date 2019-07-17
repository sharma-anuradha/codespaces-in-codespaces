using System;
using Microsoft.Extensions.Configuration;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace VsClk.ComProv.Worker
{
    class Program
    {
        public static string ServiceName = "COMPROV";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // Load configuration
            var builder = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile($"appsettings.secrets.json", optional: true)
               .AddEnvironmentVariables(ServiceName + "_");    // COMPROV_AppSettings__*
            var configuration = builder.Build();

            var loggerFactory = new DefaultLoggerFactory();
            var logger = loggerFactory.New(new LogValueSet { { "Service", "ContainerPoolWorker" } });

            logger.LogInfo("compute_provisioning_start");

            var duration = logger.StartDuration();

            #region vm_provisioning

            #endregion vm_provisioning 

            logger.AddDuration(duration);
            logger.LogInfo("compute_provisioning_completed");
        }
    }
}
