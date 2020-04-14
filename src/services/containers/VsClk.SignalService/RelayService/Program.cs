// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Microsoft.VsCloudKernel.RelayService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(context.HostingEnvironment.ContentRootPath)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
#if DEBUG
                    .AddJsonFile("appsettings.Debug.json", optional: true)
#else
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
#endif
                    .AddEnvironmentVariables()
                    .AddCommandLine(args);
                })
                .UseStartup<Startup>();
    }
}
