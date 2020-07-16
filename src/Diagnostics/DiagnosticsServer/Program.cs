// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Workers;

namespace DiagnosticsServer
{
    /// <summary>
    /// Entry point for the service.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Runs the web server.
        /// </summary>
        /// <param name="args">Arguments to change the way the host is built.
        /// Not usually needed.</param>
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var fileLogWorker = host.Services.GetRequiredService<FileLogWorker>();
            fileLogWorker.Start();
            host.Run();
        }

        /// <summary>
        /// Create Host Builder.
        /// </summary>
        /// <param name="args">Arguments to change the way the host is built.</param>
        /// <returns>An IHostBuilder.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .UseUrls("http://0.0.0.0:59330");
                });
    }
}
